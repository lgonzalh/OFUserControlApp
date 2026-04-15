using OFUserControlApp.Application.DTOs;
using OFUserControlApp.Domain.Entities;

namespace OFUserControlApp.Application.Interfaces;

/// <summary>
/// Servicio para procesar archivos Excel
/// </summary>
public interface IExcelProcessorService
{
    Task<IEnumerable<UsuarioExcel>> ExtractUsersFromExcelAsync(Stream excelStream, string fileName);
    bool IsValidExcelFile(string fileName, long fileSize);
}

/// <summary>
/// Servicio principal de usuarios
/// </summary>
public interface IUsuarioService
{
    Task<ProcesoResultado> ProcessExcelFileAsync(Stream fileStream, string fileName, string procesoId);
    Task<ProcesoResultado> GetResumenProcesoAsync(string procesoId);
    Task<ResultadoPaginado> GetResultadosPaginadosAsync(string procesoId, int pageNumber, int pageSize);
    Task<byte[]> GenerateExcelAsync(string procesoId);
    Task<byte[]> GenerateInactiveUsersExcelAsync(string procesoId);
}
