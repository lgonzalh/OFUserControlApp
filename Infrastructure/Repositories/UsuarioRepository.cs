using Microsoft.EntityFrameworkCore;
using OFUserControlApp.Domain.Entities;
using OFUserControlApp.Domain.Interfaces;
using OFUserControlApp.Infrastructure.Data;

namespace OFUserControlApp.Infrastructure.Repositories;

/// <summary>
/// Repositorio para manejo de usuarios en PostgreSQL/Supabase.
/// </summary>
public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsuarioRepository> _logger;

    public UsuarioRepository(AppDbContext context, ILogger<UsuarioRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> BulkInsertUsuariosExcelAsync(IEnumerable<UsuarioExcel> usuarios, string procesoId)
    {
        try
        {
            if (!Guid.TryParse(procesoId, out var procesoGuid))
            {
                _logger.LogError("ProcesoId invalido para carga masiva: {ProcesoId}", procesoId);
                return false;
            }

            var usuariosList = usuarios.ToList();
            var totalUsuarios = usuariosList.Count;
            _logger.LogInformation("Iniciando insercion de {Count} usuarios para proceso {ProcesoId}", totalUsuarios, procesoId);

            await CleanPreviousDataAsync(procesoGuid);

            var fechaCarga = DateTime.UtcNow;
            foreach (var usuario in usuariosList)
            {
                usuario.ProcesoId = procesoGuid;
                usuario.FuenteArchivo = string.IsNullOrWhiteSpace(usuario.FuenteArchivo) ? "Excel" : usuario.FuenteArchivo;
                usuario.CargadoEn = fechaCarga;
            }

            await _context.UsuariosExcel.AddRangeAsync(usuariosList);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Insercion completada para proceso {ProcesoId}. Usuarios: {Count}", procesoId, totalUsuarios);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en insercion para proceso {ProcesoId}: {Message}", procesoId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ExecuteCruceAsync(string procesoId)
    {
        try
        {
            if (!Guid.TryParse(procesoId, out var procesoGuid))
            {
                _logger.LogError("ProcesoId invalido para cruce: {ProcesoId}", procesoId);
                return false;
            }

            _logger.LogInformation("Ejecutando cruce para proceso {ProcesoId}", procesoId);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE FROM ""Rpt_UsuariosCruce""
                WHERE ""ProcesoId"" = {procesoGuid};");

            var insertedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""Rpt_UsuariosCruce""
                (
                    ""UsuarioExcel"",
                    ""CorreoExcel"",
                    ""EmailView"",
                    ""UPNView"",
                    ""F_Baja"",
                    ""Estado"",
                    ""ProcesoId"",
                    ""GeneradoEn""
                )
                SELECT
                    s.""Usuario"",
                    s.""Correo"",
                    v.""EMail"",
                    v.""UserPrincipalName"",
                    v.""F_Baja""::text,
                    CASE WHEN v.""EMail"" IS NOT NULL THEN 'Habilitado' ELSE 'Inactivo' END,
                    {procesoGuid},
                    CURRENT_TIMESTAMP
                FROM ""Stg_UsuariosExcel"" s
                LEFT JOIN ""View_Usuarios"" v
                    ON (s.""Correo"" IS NOT NULL AND s.""Correo"" = v.""EMail"")
                    OR (s.""Usuario"" IS NOT NULL AND s.""Usuario"" = v.""User_SO"")
                WHERE s.""ProcesoId"" = {procesoGuid};");

            var totalProcesados = await _context.UsuariosCruce.CountAsync(u => u.ProcesoId == procesoGuid);
            var habilitados = await _context.UsuariosCruce.CountAsync(u => u.ProcesoId == procesoGuid && !string.IsNullOrEmpty(u.EmailView));
            var inactivos = totalProcesados - habilitados;

            _logger.LogInformation(
                "Cruce completado para proceso {ProcesoId}. Insertados: {InsertedRows}, Total: {Total}, Habilitados: {Habilitados}, Inactivos: {Inactivos}",
                procesoId,
                insertedRows,
                totalProcesados,
                habilitados,
                inactivos);

            return totalProcesados > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando cruce para proceso {ProcesoId}: {Message}", procesoId, ex.Message);
            return false;
        }
    }

    public async Task<IEnumerable<UsuarioCruce>> GetResultadosCruceAsync(string procesoId, int pageNumber, int pageSize)
    {
        if (!Guid.TryParse(procesoId, out var procesoGuid))
        {
            return Enumerable.Empty<UsuarioCruce>();
        }

        var skip = (pageNumber - 1) * pageSize;

        return await _context.UsuariosCruce
            .Where(u => u.ProcesoId == procesoGuid)
            .OrderBy(u => u.UsuarioExcel)
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetTotalResultadosAsync(string procesoId)
    {
        if (!Guid.TryParse(procesoId, out var procesoGuid))
        {
            return 0;
        }

        return await _context.UsuariosCruce.CountAsync(u => u.ProcesoId == procesoGuid);
    }

    public async Task<(int total, int habilitados, int inactivos)> GetResumenAsync(string procesoId)
    {
        if (!Guid.TryParse(procesoId, out var procesoGuid))
        {
            return (0, 0, 0);
        }

        var total = await _context.UsuariosCruce.CountAsync(u => u.ProcesoId == procesoGuid);
        var habilitados = await _context.UsuariosCruce.CountAsync(u => u.ProcesoId == procesoGuid && !string.IsNullOrEmpty(u.EmailView));
        var inactivos = total - habilitados;
        return (total, habilitados, inactivos);
    }

    public async Task LimpiarDatosTemporalesAsync(string procesoId)
    {
        if (!Guid.TryParse(procesoId, out var procesoGuid))
        {
            _logger.LogWarning("No se limpia porque el ProcesoId es invalido: {ProcesoId}", procesoId);
            return;
        }

        _logger.LogInformation("Iniciando limpieza para proceso {ProcesoId}", procesoId);

        var rptRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM ""Rpt_UsuariosCruce""
            WHERE ""ProcesoId"" = {procesoGuid};");

        var stgRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM ""Stg_UsuariosExcel""
            WHERE ""ProcesoId"" = {procesoGuid};");

        _logger.LogInformation(
            "Limpieza completada para proceso {ProcesoId}. Rpt_UsuariosCruce: {RptRows}, Stg_UsuariosExcel: {StgRows}",
            procesoId,
            rptRows,
            stgRows);
    }

    private async Task CleanPreviousDataAsync(Guid procesoGuid)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM ""Rpt_UsuariosCruce""
            WHERE ""ProcesoId"" = {procesoGuid};");

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM ""Stg_UsuariosExcel""
            WHERE ""ProcesoId"" = {procesoGuid};");
    }
}
