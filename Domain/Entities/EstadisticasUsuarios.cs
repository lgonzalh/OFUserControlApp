namespace OFUserControlApp.Domain.Entities;

/// <summary>
/// Representa las estadísticas de usuarios por proceso
/// Mapea la vista vw_EstadisticasUsuarios
/// </summary>
public sealed class EstadisticasUsuarios
{
    public Guid ProcesoId { get; set; }
    public int? TotalRegistros { get; set; }
    public int? Habilitados { get; set; }
    public int? Inactivos { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
