using OFUserControlApp.Application.DTOs;

namespace OFUserControlApp.Application.Interfaces;

/// <summary>
/// Servicio de progreso en tiempo real
/// </summary>
public interface IProgresoService
{
    Task NotifyProgressAsync(string procesoId, int porcentaje, string etapa, string mensaje);
    void SetProgreso(string procesoId, ProgresoInfo progreso);
    ProgresoInfo? GetProgreso(string procesoId);
    Task StoreResultadosAsync(string procesoId, List<UsuarioResultadoDTO> resultados);
    Task<List<UsuarioResultadoDTO>?> GetResultadosAsync(string procesoId);
}
