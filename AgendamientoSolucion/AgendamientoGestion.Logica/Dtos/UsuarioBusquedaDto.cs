namespace AgendamientoGestion.Logica.Dtos;

/// <summary>
/// Parámetros opcionales de GET /api/Usuario. Si no llega ninguno, el endpoint devuelve el array completo
/// (comportamiento anterior); si llega alguno, devuelve una página y la cabecera X-Total-Count.
/// </summary>
public class UsuarioBusquedaDto
{
    public const int PageSizePorDefecto = 20;
    public const int PageSizeMaximo = 100;
    public const int LongitudMaximaQ = 100;

    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Q { get; set; }
    public string? Rol { get; set; }
    public bool? Activo { get; set; }

    public bool HayParametros =>
        Page.HasValue || PageSize.HasValue || !string.IsNullOrWhiteSpace(Q) || !string.IsNullOrWhiteSpace(Rol) || Activo.HasValue;
}

/// <summary>Una página de resultados y el total de elementos que cumplen el filtro.</summary>
public record PaginaDto<T>(List<T> Items, int Total);
