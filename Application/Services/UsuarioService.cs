using OFUserControlApp.Application.DTOs;
using OFUserControlApp.Application.Interfaces;
using OFUserControlApp.Domain.Interfaces;
using OFUserControlApp.Domain.Entities;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace OFUserControlApp.Application.Services;

/// <summary>
/// Servicio principal de usuarios implementando principios SOLID
/// </summary>
public sealed class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IExcelProcessorService _excelProcessorService;
    private readonly IProgresoService _progresoService;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IExcelProcessorService excelProcessorService,
        IProgresoService progresoService,
        ILogger<UsuarioService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _excelProcessorService = excelProcessorService;
        _progresoService = progresoService;
        _logger = logger;
    }

    public async Task<ProcesoResultado> ProcessExcelFileAsync(Stream fileStream, string fileName, string procesoId)
    {
        try
        {
            _logger.LogInformation("Iniciando procesamiento de archivo {FileName} con procesoId {ProcesoId}", fileName, procesoId);

            // Paso 1: Extraer usuarios del Excel
            await _progresoService.NotifyProgressAsync(procesoId, 5, "Validación", "Validando archivo Excel...");
            await _progresoService.NotifyProgressAsync(procesoId, 10, "Extracción", "Extrayendo usuarios del archivo Excel...");
            
            var usuarios = await _excelProcessorService.ExtractUsersFromExcelAsync(fileStream, fileName);
            var totalUsuarios = usuarios.Count();
            
            await _progresoService.NotifyProgressAsync(procesoId, 20, "Extracción", 
                $"Extracción completada: {totalUsuarios:N0} usuarios encontrados en el archivo Excel");
            
            if (!usuarios.Any())
            {
                await _progresoService.NotifyProgressAsync(procesoId, 0, "Error", 
                    "No se encontraron usuarios válidos en el archivo Excel. Verifique que el archivo contenga las columnas 'Usuario' y 'Correo'");
                
                return new ProcesoResultado
                {
                    ProcesoId = procesoId,
                    Archivo = fileName,
                    Total = 0,
                    Habilitados = 0,
                    Inactivos = 0,
                    Exitoso = false,
                    Mensaje = "No se encontraron usuarios en el archivo Excel"
                };
            }

            // Paso 2: Insertar usuarios en staging
            await _progresoService.NotifyProgressAsync(procesoId, 30, "Conexión BD", "Conectando a base de datos...");
            await _progresoService.NotifyProgressAsync(procesoId, 35, "Carga", "Preparando inserción masiva...");
            await _progresoService.NotifyProgressAsync(procesoId, 40, "Carga", $"Cargando {totalUsuarios:N0} usuarios en base de datos...");
            
            var insertSuccess = await _usuarioRepository.BulkInsertUsuariosExcelAsync(usuarios, procesoId);
            
            if (!insertSuccess)
            {
                await _progresoService.NotifyProgressAsync(procesoId, 0, "Error", 
                    "Error al cargar usuarios en base de datos. Verifique que las tablas Stg_UsuariosExcel y Rpt_UsuariosCruce existan y tenga permisos de INSERT/DELETE");
                
                return new ProcesoResultado
                {
                    ProcesoId = procesoId,
                    Archivo = fileName,
                    Total = usuarios.Count(),
                    Habilitados = 0,
                    Inactivos = 0,
                    Exitoso = false,
                    Mensaje = "Error al cargar usuarios en base de datos. Ejecute el script CreateTables_PostgreSQL.sql en su base PostgreSQL/Supabase"
                };
            }
            
            await _progresoService.NotifyProgressAsync(procesoId, 50, "Carga", 
                $"Carga completada: {totalUsuarios:N0} usuarios cargados exitosamente en la base de datos");

            // Paso 3: Ejecutar cruce con View_Usuarios
            await _progresoService.NotifyProgressAsync(procesoId, 60, "Cruce", "Conectando a Vista_Usuarios...");
            await _progresoService.NotifyProgressAsync(procesoId, 65, "Cruce", "Ejecutando stored procedure de cruce...");
            await _progresoService.NotifyProgressAsync(procesoId, 70, "Cruce", "Cruzando usuarios con directorio activo...");
            
            var cruceSuccess = await _usuarioRepository.ExecuteCruceAsync(procesoId);
            
            if (!cruceSuccess)
            {
                await _progresoService.NotifyProgressAsync(procesoId, 0, "Error", 
                    "Error al ejecutar cruce de usuarios. Verifique que la vista View_Usuarios exista y sea accesible en PostgreSQL");
                
                return new ProcesoResultado
                {
                    ProcesoId = procesoId,
                    Archivo = fileName,
                    Total = usuarios.Count(),
                    Habilitados = 0,
                    Inactivos = 0,
                    Exitoso = false,
                    Mensaje = "Error al ejecutar cruce de usuarios"
                };
            }
            
            await _progresoService.NotifyProgressAsync(procesoId, 80, "Cruce", 
                "Cruce completado: Usuarios cruzados exitosamente con el directorio activo");

            // Paso 4: Obtener resultados completos ANTES de limpiar
            await _progresoService.NotifyProgressAsync(procesoId, 85, "Finalización", "Obteniendo resultados completos...");
            
            var resultadosCompletos = await _usuarioRepository.GetResultadosCruceAsync(procesoId, 1, int.MaxValue);
            var resultadosList = resultadosCompletos.ToList(); // Almacenar en memoria
            
            // Convertir a DTOs para almacenamiento en memoria
            var resultadosDTO = resultadosList.Select(UsuarioResultadoDTO.FromUsuarioCruce).ToList();
            
            // Calcular estadísticas desde los datos en memoria
            var total = resultadosDTO.Count;
            var habilitados = resultadosDTO.Count(r => r.ExisteEnView);
            var inactivos = total - habilitados;

            await _progresoService.NotifyProgressAsync(procesoId, 90, "Finalización", 
                $"Resumen: {total:N0} total, {habilitados:N0} habilitados, {inactivos:N0} inactivos");

            // Paso 5: Almacenar resultados en el servicio de progreso para acceso posterior
            await _progresoService.StoreResultadosAsync(procesoId, resultadosDTO);

            await _progresoService.NotifyProgressAsync(procesoId, 95, "Finalización", 
                $"Proceso finalizado exitosamente. {total:N0} usuarios procesados");

            // Paso 6: Limpiar datos de las tablas de soporte DESPUÉS de obtener los resultados
            await _progresoService.NotifyProgressAsync(procesoId, 100, "Limpieza", "Limpiando datos temporales...");
            _logger.LogInformation("Iniciando limpieza de datos temporales para proceso {ProcesoId}", procesoId);
            
            try
            {
                await _usuarioRepository.LimpiarDatosTemporalesAsync(procesoId);
                _logger.LogInformation("Limpieza de datos temporales completada exitosamente para proceso {ProcesoId}", procesoId);
                await _progresoService.NotifyProgressAsync(procesoId, 100, "Completado", 
                    $"Proceso finalizado exitosamente. {total:N0} usuarios procesados. Datos temporales limpiados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la limpieza de datos temporales para proceso {ProcesoId}: {Message}", procesoId, ex.Message);
                await _progresoService.NotifyProgressAsync(procesoId, 100, "Completado", 
                    $"Proceso finalizado exitosamente. {total:N0} usuarios procesados. Error en limpieza: {ex.Message}");
            }

            return new ProcesoResultado
            {
                ProcesoId = procesoId,
                Archivo = fileName,
                Total = total,
                Habilitados = habilitados,
                Inactivos = inactivos,
                Exitoso = true,
                Mensaje = "Proceso completado exitosamente"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando archivo {FileName} con procesoId {ProcesoId}", fileName, procesoId);
            await _progresoService.NotifyProgressAsync(procesoId, 0, "Error", $"Error: {ex.Message}");
            
            return new ProcesoResultado
            {
                ProcesoId = procesoId,
                Archivo = fileName,
                Total = 0,
                Habilitados = 0,
                Inactivos = 0,
                Exitoso = false,
                Mensaje = $"Error inesperado: {ex.Message}"
            };
        }
    }

    public async Task<ProcesoResultado> GetResumenProcesoAsync(string procesoId)
    {
        // Intentar obtener datos desde el servicio de progreso (datos en memoria)
        var resultadosAlmacenados = await _progresoService.GetResultadosAsync(procesoId);
        
        if (resultadosAlmacenados != null && resultadosAlmacenados.Any())
        {
            var total = resultadosAlmacenados.Count;
            var habilitados = resultadosAlmacenados.Count(r => r.ExisteEnView);
            var inactivos = total - habilitados;
            
            return new ProcesoResultado
            {
                ProcesoId = procesoId,
                Archivo = "Archivo procesado",
                Total = total,
                Habilitados = habilitados,
                Inactivos = inactivos,
                Exitoso = total > 0,
                Mensaje = total > 0 ? "Datos disponibles" : "No hay datos para este proceso"
            };
        }
        
        // Fallback: intentar obtener desde la base de datos (por si acaso)
        try
        {
            var (total, habilitados, inactivos) = await _usuarioRepository.GetResumenAsync(procesoId);
            
            return new ProcesoResultado
            {
                ProcesoId = procesoId,
                Archivo = "Archivo procesado",
                Total = total,
                Habilitados = habilitados,
                Inactivos = inactivos,
                Exitoso = total > 0,
                Mensaje = total > 0 ? "Datos disponibles" : "No hay datos para este proceso"
            };
        }
        catch
        {
            return new ProcesoResultado
            {
                ProcesoId = procesoId,
                Archivo = "Archivo procesado",
                Total = 0,
                Habilitados = 0,
                Inactivos = 0,
                Exitoso = false,
                Mensaje = "No hay datos para este proceso"
            };
        }
    }

    public async Task<ResultadoPaginado> GetResultadosPaginadosAsync(string procesoId, int pageNumber, int pageSize)
    {
        // Intentar obtener datos desde el servicio de progreso (datos en memoria)
        var resultadosAlmacenados = await _progresoService.GetResultadosAsync(procesoId);
        
        if (resultadosAlmacenados != null && resultadosAlmacenados.Any())
        {
            var skip = (pageNumber - 1) * pageSize;
            var resultadosPaginados = resultadosAlmacenados
                .OrderBy(r => r.Usuario)
                .Skip(skip)
                .Take(pageSize);

            var items = resultadosPaginados.Select(r => new UsuarioResultadoItem
            {
                Usuario = r.Usuario,
                Nombre = !string.IsNullOrEmpty(r.NombreCompleto) ? r.NombreCompleto : r.Usuario,
                Email = r.Correo,
                Estado = r.Estado,
                EstaHabilitado = r.ExisteEnView,
                FechaVerificacion = r.FechaProceso
            });

            return new ResultadoPaginado
            {
                Items = items,
                TotalItems = resultadosAlmacenados.Count,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }
        
        // Fallback: intentar obtener desde la base de datos (por si acaso)
        try
        {
            var resultados = await _usuarioRepository.GetResultadosCruceAsync(procesoId, pageNumber, pageSize);
            var totalItems = await _usuarioRepository.GetTotalResultadosAsync(procesoId);

            var items = resultados.Select(r => new UsuarioResultadoItem
            {
                Usuario = r.UsuarioExcel ?? string.Empty,
                Nombre = r.UsuarioExcel ?? string.Empty, // No hay NombreCompleto en la estructura real
                Email = r.CorreoExcel ?? string.Empty,
                Estado = r.Estado ?? string.Empty,
                EstaHabilitado = !string.IsNullOrEmpty(r.EmailView),
                FechaVerificacion = r.GeneradoEn
            });

            return new ResultadoPaginado
            {
                Items = items,
                TotalItems = totalItems,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }
        catch
        {
            return new ResultadoPaginado
            {
                Items = Enumerable.Empty<UsuarioResultadoItem>(),
                TotalItems = 0,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }
    }

    public async Task<byte[]> GenerateExcelAsync(string procesoId)
    {
        // Intentar obtener datos desde el servicio de progreso (datos en memoria)
        var resultadosAlmacenados = await _progresoService.GetResultadosAsync(procesoId);
        
        IEnumerable<UsuarioResultadoDTO> resultados;
        
        if (resultadosAlmacenados != null && resultadosAlmacenados.Any())
        {
            resultados = resultadosAlmacenados.OrderBy(r => r.Usuario);
        }
        else
        {
            // Fallback: intentar obtener desde la base de datos (por si acaso)
            try
            {
                var resultadosBD = await _usuarioRepository.GetResultadosCruceAsync(procesoId, 1, int.MaxValue);
                resultados = resultadosBD.Select(UsuarioResultadoDTO.FromUsuarioCruce).OrderBy(r => r.Usuario);
            }
            catch
            {
                // Si no hay datos, retornar Excel vacío
                using var emptyPackage = new ExcelPackage();
                var emptyWorksheet = emptyPackage.Workbook.Worksheets.Add("Resultados");
                
                // Encabezados
                emptyWorksheet.Cells[1, 1].Value = "Usuario";
                emptyWorksheet.Cells[1, 2].Value = "Nombre Completo";
                emptyWorksheet.Cells[1, 3].Value = "Correo";
                emptyWorksheet.Cells[1, 4].Value = "Estado";
                emptyWorksheet.Cells[1, 5].Value = "Fecha Proceso";
                
                // Estilo de encabezados
                var emptyHeaderRange = emptyWorksheet.Cells[1, 1, 1, 5];
                emptyHeaderRange.Style.Font.Bold = true;
                emptyHeaderRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                emptyHeaderRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                
                // Aplicar formato de fecha a la columna "Fecha Proceso" (aunque esté vacía)
                var emptyFechaColumn = emptyWorksheet.Cells[1, 5, 1, 5];
                emptyFechaColumn.Style.Numberformat.Format = "dd/mm/yyyy";
                
                emptyWorksheet.Cells.AutoFitColumns();
                return emptyPackage.GetAsByteArray();
            }
        }
        
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Resultados");
        
        // Encabezados
        worksheet.Cells[1, 1].Value = "Usuario";
        worksheet.Cells[1, 2].Value = "Nombre Completo";
        worksheet.Cells[1, 3].Value = "Correo";
        worksheet.Cells[1, 4].Value = "Estado";
        worksheet.Cells[1, 5].Value = "Fecha Proceso";
        
        // Estilo de encabezados
        var headerRange = worksheet.Cells[1, 1, 1, 5];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        
        // Datos
        var row = 2;
        foreach (var resultado in resultados)
        {
            worksheet.Cells[row, 1].Value = resultado.Usuario;
            worksheet.Cells[row, 2].Value = !string.IsNullOrEmpty(resultado.NombreCompleto) ? resultado.NombreCompleto : resultado.Usuario;
            worksheet.Cells[row, 3].Value = resultado.Correo;
            worksheet.Cells[row, 4].Value = resultado.Estado;
            worksheet.Cells[row, 5].Value = resultado.FechaProceso;
            row++;
        }
        
        // Aplicar formato de fecha a la columna "Fecha Proceso"
        var fechaColumn = worksheet.Cells[2, 5, row - 1, 5];
        fechaColumn.Style.Numberformat.Format = "dd/mm/yyyy";
        
        // Autoajustar columnas
        worksheet.Cells.AutoFitColumns();
        
        return package.GetAsByteArray();
    }

    public async Task<byte[]> GenerateInactiveUsersExcelAsync(string procesoId)
    {
        // Intentar obtener datos desde el servicio de progreso (datos en memoria)
        var resultadosAlmacenados = await _progresoService.GetResultadosAsync(procesoId);
        
        IEnumerable<UsuarioResultadoDTO> resultados;
        
        if (resultadosAlmacenados != null && resultadosAlmacenados.Any())
        {
            // Filtrar solo usuarios inactivos
            resultados = resultadosAlmacenados
                .Where(r => !r.ExisteEnView) // Solo usuarios que NO existen en la vista (inactivos)
                .OrderBy(r => r.Usuario);
        }
        else
        {
            // Fallback: intentar obtener desde la base de datos (por si acaso)
            try
            {
                var todosLosResultados = await _usuarioRepository.GetResultadosCruceAsync(procesoId, 1, int.MaxValue);
                // Filtrar solo usuarios inactivos
                resultados = todosLosResultados
                    .Where(r => string.IsNullOrEmpty(r.EmailView)) // Solo usuarios que NO existen en la vista (inactivos)
                    .Select(UsuarioResultadoDTO.FromUsuarioCruce)
                    .OrderBy(r => r.Usuario);
            }
            catch
            {
                // Si no hay datos, retornar Excel vacío
                using var emptyPackage = new ExcelPackage();
                var emptyWorksheet = emptyPackage.Workbook.Worksheets.Add("Usuarios Inactivos");
                
                // Encabezados
                emptyWorksheet.Cells[1, 1].Value = "Usuario";
                emptyWorksheet.Cells[1, 2].Value = "Nombre Completo";
                emptyWorksheet.Cells[1, 3].Value = "Correo";
                emptyWorksheet.Cells[1, 4].Value = "Estado";
                emptyWorksheet.Cells[1, 5].Value = "Fecha Proceso";
                
                // Estilo de encabezados
                var emptyHeaderRange = emptyWorksheet.Cells[1, 1, 1, 5];
                emptyHeaderRange.Style.Font.Bold = true;
                emptyHeaderRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                emptyHeaderRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                
                // Aplicar formato de fecha a la columna "Fecha Proceso" (aunque esté vacía)
                var emptyFechaColumn = emptyWorksheet.Cells[1, 5, 1, 5];
                emptyFechaColumn.Style.Numberformat.Format = "dd/mm/yyyy";
                
                emptyWorksheet.Cells.AutoFitColumns();
                return emptyPackage.GetAsByteArray();
            }
        }
        
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Usuarios Inactivos");
        
        // Encabezados
        worksheet.Cells[1, 1].Value = "Usuario";
        worksheet.Cells[1, 2].Value = "Nombre Completo";
        worksheet.Cells[1, 3].Value = "Correo";
        worksheet.Cells[1, 4].Value = "Estado";
        worksheet.Cells[1, 5].Value = "Fecha Proceso";
        
        // Estilo de encabezados
        var headerRange = worksheet.Cells[1, 1, 1, 5];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        
        // Datos
        var row = 2;
        foreach (var resultado in resultados)
        {
            worksheet.Cells[row, 1].Value = resultado.Usuario;
            worksheet.Cells[row, 2].Value = !string.IsNullOrEmpty(resultado.NombreCompleto) ? resultado.NombreCompleto : resultado.Usuario;
            worksheet.Cells[row, 3].Value = resultado.Correo;
            worksheet.Cells[row, 4].Value = "Inactivo"; // Forzar estado como "Inactivo"
            worksheet.Cells[row, 5].Value = resultado.FechaProceso;
            row++;
        }
        
        // Aplicar formato de fecha a la columna "Fecha Proceso"
        if (row > 2) // Solo si hay datos
        {
            var fechaColumn = worksheet.Cells[2, 5, row - 1, 5];
            fechaColumn.Style.Numberformat.Format = "dd/mm/yyyy";
        }
        
        // Autoajustar columnas
        worksheet.Cells.AutoFitColumns();
        
        return package.GetAsByteArray();
    }
}
