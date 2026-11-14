using System.ComponentModel.DataAnnotations;

namespace AgendamientoGestion.Logica.Dtos
{
    public class RetroalimentacionCreateDto
    {
        /// <summary>
        /// Opcional: puede ser null o "". La app MAUI envía "" cuando el docente no escribe comentario;
        /// antes [Required] lo rechazaba con 400 y la tutoría no llegaba a finalizarse.
        /// Se guarda como "" porque la columna es NOT NULL.
        /// </summary>
        [StringLength(300, ErrorMessage = "El comentario no puede exceder 300 caracteres")]
        public string? Comentario { get; set; }

        [Required(ErrorMessage = "La calificación es obligatoria")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public decimal Calificacion { get; set; }

        [Required(ErrorMessage = "El ID de la tutoría es obligatorio")]
        public int TutoriaId { get; set; }

        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }
    }
}
