namespace OFUserControlApp.Domain.Entities;

/// <summary>
/// Mapea la vista existente View_Usuarios
/// Estructura basada en la consulta SQL proporcionada
/// </summary>
public sealed class ViewUsuario
{
    public int Id { get; set; }
    public string? EMail { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? User_SO { get; set; }
    public DateTime? F_Alta { get; set; }
    public int? Cod_Puesto { get; set; }
    public int? Cod_Unidad { get; set; }
    public string? Cedula { get; set; }
    public int? Cod_Jefe { get; set; }
    public int? COD_USUARIO_MIGRACION { get; set; }
    public int? COD_JEFE_MIGRACION { get; set; }
    public int? cod_status_usuario { get; set; }
    public bool? IsAdmin { get; set; }
    public bool? IsUnidad { get; set; }
    public bool? DebeRegistrarEnTS { get; set; }
    public int? IdStatusADAzure { get; set; }
    public int? IdAzureObject { get; set; }
    public DateTime? F_Baja { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? JobTitle { get; set; }
    
    // Propiedades de compatibilidad para el mapeo actual
    public string Usuario => User_SO ?? string.Empty;
    public string Correo => EMail ?? string.Empty;
}
