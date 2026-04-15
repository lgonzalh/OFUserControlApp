using OFUserControlApp.Domain.Entities;

namespace OFUserControlApp.Domain.Interfaces;

/// <summary>
/// Repositorio para manejo de usuarios
/// </summary>
public interface IUsuarioRepository
{
    Task<bool> BulkInsertUsuariosExcelAsync(IEnumerable<UsuarioExcel> usuarios, string procesoId);
    Task<bool> ExecuteCruceAsync(string procesoId);
    Task<IEnumerable<UsuarioCruce>> GetResultadosCruceAsync(string procesoId, int pageNumber, int pageSize);
    Task<int> GetTotalResultadosAsync(string procesoId);
    Task<(int total, int habilitados, int inactivos)> GetResumenAsync(string procesoId);
    Task LimpiarDatosTemporalesAsync(string procesoId);
}
