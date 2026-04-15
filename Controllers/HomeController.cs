using Microsoft.AspNetCore.Mvc;
using OFUserControlApp.Application.Interfaces;

namespace OFUserControlApp.Controllers;

/// <summary>
/// Controlador principal de la aplicación
/// </summary>
public sealed class HomeController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IUsuarioService usuarioService, ILogger<HomeController> logger)
    {
        _usuarioService = usuarioService;
        _logger = logger;
    }

    /// <summary>
    /// Página principal
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Página de resultados
    /// </summary>
    public async Task<IActionResult> Results(string procesoId, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(procesoId))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            const int pageSize = 20;
            var resumen = await _usuarioService.GetResumenProcesoAsync(procesoId);
            var resultados = await _usuarioService.GetResultadosPaginadosAsync(procesoId, page, pageSize);

            ViewBag.ProcesoId = procesoId;
            ViewBag.Resumen = resumen;
            ViewBag.Resultados = resultados;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando resultados para proceso {ProcesoId}", procesoId);
            TempData["Error"] = "Error cargando los resultados. Por favor, inténtelo nuevamente.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Descargar resultados en Excel
    /// </summary>
    public async Task<IActionResult> DownloadExcel(string procesoId)
    {
        if (string.IsNullOrWhiteSpace(procesoId))
        {
            return BadRequest("ID de proceso requerido");
        }

        try
        {
            var excelBytes = await _usuarioService.GenerateExcelAsync(procesoId);
            var fileName = $"resultados_usuarios_{procesoId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando Excel para proceso {ProcesoId}", procesoId);
            TempData["Error"] = "Error generando el archivo Excel.";
            return RedirectToAction(nameof(Results), new { procesoId });
        }
    }
}