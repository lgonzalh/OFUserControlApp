namespace OFUserControlApp.Application.DTOs;

/// <summary>
/// DTO para representar los resultados del cruce de usuarios
/// </summary>
public sealed class UsuarioResultadoDTO
{
    public string Usuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool ExisteEnView { get; set; }
    public DateTime FechaProceso { get; set; }
    
    /// <summary>
    /// Constructor para crear DTO desde entidad UsuarioCruce
    /// </summary>
    public static UsuarioResultadoDTO FromUsuarioCruce(Domain.Entities.UsuarioCruce cruce)
    {
        return new UsuarioResultadoDTO
        {
            Usuario = cruce.UsuarioExcel ?? string.Empty,
            Correo = cruce.CorreoExcel ?? string.Empty,
            NombreCompleto = string.Empty, // No disponible en la estructura real
            Estado = cruce.Estado ?? string.Empty,
            ExisteEnView = !string.IsNullOrEmpty(cruce.EmailView),
            FechaProceso = cruce.GeneradoEn
        };
    }
}
