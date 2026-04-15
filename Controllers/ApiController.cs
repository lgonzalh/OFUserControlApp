using Microsoft.AspNetCore.Mvc;
using OFUserControlApp.Application.Interfaces;
using OFUserControlApp.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace OFUserControlApp.Controllers;

/// <summary>
/// Controlador API para operaciones asíncronas
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ApiController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IExcelProcessorService _excelProcessorService;
    private readonly IProgresoService _progresoService;
    private readonly ILogger<ApiController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ApiController(
        IUsuarioService usuarioService,
        IExcelProcessorService excelProcessorService,
        IProgresoService progresoService,
        ILogger<ApiController> logger,
        IServiceProvider serviceProvider)
    {
        _usuarioService = usuarioService;
        _excelProcessorService = excelProcessorService;
        _progresoService = progresoService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Procesar archivo Excel
    /// </summary>
    [HttpPost("process-excel")]
    public async Task<IActionResult> ProcessExcel(IFormFile file)
    {
        try
        {
            // Validaciones básicas
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Debe seleccionar un archivo válido" });
            }

            if (!_excelProcessorService.IsValidExcelFile(file.FileName, file.Length))
            {
                return BadRequest(new { success = false, message = "Archivo inválido. Solo se permiten archivos Excel (.xls, .xlsx) de máximo 50MB" });
            }

            // Generar ID único para el proceso
            var procesoId = Guid.NewGuid().ToString("D").ToUpperInvariant();
            
            _logger.LogInformation("Iniciando procesamiento de archivo {FileName} con procesoId {ProcesoId}", 
                file.FileName, procesoId);

            // Copiar el stream ANTES de iniciar el procesamiento en segundo plano
            using var originalStream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await originalStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            // Procesar archivo en tarea en segundo plano
            _ = Task.Run(async () =>
            {
                try
                {
                    // Crear un nuevo scope para el procesamiento en segundo plano
                    using var scope = _serviceProvider.CreateScope();
                    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
                    var progresoService = scope.ServiceProvider.GetRequiredService<IProgresoService>();
                    
                    // Crear una copia del MemoryStream para el procesamiento
                    using var processingStream = new MemoryStream(memoryStream.ToArray());
                    await usuarioService.ProcessExcelFileAsync(processingStream, file.FileName, procesoId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en procesamiento en segundo plano para proceso {ProcesoId}: {Message}", procesoId, ex.Message);
                    
                    var errorMessage = ex switch
                    {
                        InvalidOperationException => $"Error de procesamiento: {ex.Message}",
                        DbException => $"Error de base de datos: {ex.Message}",
                        _ => $"Error inesperado: {ex.Message}"
                    };
                    
                    // Usar el progresoService del scope si está disponible, sino el del controlador
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var progresoService = scope.ServiceProvider.GetRequiredService<IProgresoService>();
                        await progresoService.NotifyProgressAsync(procesoId, 0, "Error", errorMessage);
                    }
                    catch
                    {
                        await _progresoService.NotifyProgressAsync(procesoId, 0, "Error", errorMessage);
                    }
                }
            });

            return Ok(new { success = true, procesoId, message = "Procesamiento iniciado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error iniciando procesamiento de archivo {FileName}", file?.FileName);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener progreso del proceso
    /// </summary>
    [HttpGet("progress/{procesoId}")]
    public IActionResult GetProgress(string procesoId)
    {
        try
        {
            var progreso = _progresoService.GetProgreso(procesoId);
            
            if (progreso == null)
            {
                return NotFound(new { success = false, message = "Proceso no encontrado" });
            }

            return Ok(new { success = true, progreso });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo progreso para proceso {ProcesoId}", procesoId);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener resumen del proceso
    /// </summary>
    [HttpGet("summary/{procesoId}")]
    public async Task<IActionResult> GetSummary(string procesoId)
    {
        try
        {
            var resumen = await _usuarioService.GetResumenProcesoAsync(procesoId);
            return Ok(new { success = true, resumen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo resumen para proceso {ProcesoId}", procesoId);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener resultados paginados
    /// </summary>
    [HttpGet("results/{procesoId}")]
    public async Task<IActionResult> GetResults(string procesoId, int page = 1, int pageSize = 20)
    {
        try
        {
            var resultados = await _usuarioService.GetResultadosPaginadosAsync(procesoId, page, pageSize);
            return Ok(new { success = true, resultados });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo resultados para proceso {ProcesoId}", procesoId);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Descargar resultados en formato Excel
    /// </summary>
    [HttpGet("download-excel/{procesoId}")]
    public async Task<IActionResult> DownloadExcel(string procesoId)
    {
        try
        {
            var excelData = await _usuarioService.GenerateExcelAsync(procesoId);
            var fileName = $"resultados_cruce_{procesoId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando Excel para proceso {ProcesoId}", procesoId);
            return StatusCode(500, new { success = false, message = "Error generando archivo Excel" });
        }
    }

    /// <summary>
    /// Descargar solo usuarios inactivos en formato Excel
    /// </summary>
    [HttpGet("download-inactive-excel/{procesoId}")]
    public async Task<IActionResult> DownloadInactiveExcel(string procesoId)
    {
        try
        {
            var excelData = await _usuarioService.GenerateInactiveUsersExcelAsync(procesoId);
            var fileName = $"usuarios_inactivos_{procesoId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando Excel de usuarios inactivos para proceso {ProcesoId}", procesoId);
            return StatusCode(500, new { success = false, message = "Error generando archivo Excel de usuarios inactivos" });
        }
    }
}
