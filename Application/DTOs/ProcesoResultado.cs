namespace OFUserControlApp.Application.DTOs;

/// <summary>
/// Resultado del procesamiento
/// </summary>
public sealed record ProcesoResultado
{
    public required string ProcesoId { get; init; }
    public required string Archivo { get; init; }
    public int Total { get; init; }
    public int Habilitados { get; init; }
    public int Inactivos { get; init; }
    public bool Exitoso { get; init; }
    public string? Mensaje { get; init; }
}

/// <summary>
/// Item de resultado paginado
/// </summary>
public sealed record UsuarioResultadoItem
{
    public required string Usuario { get; init; }
    public required string Nombre { get; init; }
    public required string Email { get; init; }
    public required string Estado { get; init; }
    public bool EstaHabilitado { get; init; }
    public DateTime FechaVerificacion { get; init; }
}

/// <summary>
/// Resultado paginado
/// </summary>
public sealed record ResultadoPaginado
{
    public required IEnumerable<UsuarioResultadoItem> Items { get; init; }
    public int TotalItems { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}

/// <summary>
/// Progreso del proceso
/// </summary>
public sealed record ProgresoInfo
{
    public required string ProcesoId { get; init; }
    public int Porcentaje { get; init; }
    public required string Etapa { get; init; }
    public required string Mensaje { get; init; }
}
