using System.ComponentModel.DataAnnotations;

namespace AgendamientoGestion.Logica.Dtos;

/// <summary>
/// Cuerpo OPCIONAL de PUT /api/Tutoria/{id}/finalizar. Si llega con calificación, se crea la retroalimentación
/// (autor = usuario del token) y se finaliza la tutoría en una sola transacción. Sin cuerpo = comportamiento anterior.
/// </summary>
public class FinalizarTutoriaDto
{
    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
    public decimal? Calificacion { get; set; }

    [StringLength(300, ErrorMessage = "El comentario no puede exceder 300 caracteres")]
    public string? Comentario { get; set; }
}
