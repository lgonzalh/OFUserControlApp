namespace OFUserControlApp.Domain.Entities;

/// <summary>
/// Representa un usuario extraído de Excel
/// Mapea la tabla Stg_UsuariosExcel
/// </summary>
public sealed class UsuarioExcel
{
    public long Id { get; set; }
    public Guid ProcesoId { get; set; }
    public string? Usuario { get; set; }
    public string? Correo { get; set; }
    public string FuenteArchivo { get; set; } = string.Empty;
    public byte[]? HashContenido { get; set; }
    public DateTime CargadoEn { get; set; }
}
