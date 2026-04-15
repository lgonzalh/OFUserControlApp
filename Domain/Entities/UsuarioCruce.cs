namespace OFUserControlApp.Domain.Entities;

/// <summary>
/// Representa el resultado del cruce de usuarios
/// Mapea la tabla Rpt_UsuariosCruce
/// </summary>
public sealed class UsuarioCruce
{
    public long Id { get; set; }
    public string? UsuarioExcel { get; set; }
    public string? CorreoExcel { get; set; }
    public string? EmailView { get; set; }
    public string? UPNView { get; set; }
    public string? F_Baja { get; set; }
    public string Estado { get; set; } = string.Empty;
    public Guid ProcesoId { get; set; }
    public DateTime GeneradoEn { get; set; }
}
