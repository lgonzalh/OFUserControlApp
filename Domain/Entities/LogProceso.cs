namespace OFUserControlApp.Domain.Entities;

/// <summary>
/// Representa el log de procesos ejecutados
/// Mapea la tabla LogProceso
/// </summary>
public sealed class LogProceso
{
    public long Id { get; set; }
    public Guid ProcesoId { get; set; }
    public string Etapa { get; set; } = string.Empty;
    public string? Mensaje { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
}
