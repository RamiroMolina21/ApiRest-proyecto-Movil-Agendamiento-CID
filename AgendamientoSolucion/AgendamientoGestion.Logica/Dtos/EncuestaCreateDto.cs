using System.ComponentModel.DataAnnotations;

namespace AgendamientoGestion.Logica.Dtos
{
    public class EncuestaCreateDto
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; }

        [Required(ErrorMessage = "La calificación es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int Calificacion { get; set; }

        [StringLength(300, ErrorMessage = "El comentario no puede superar los 300 caracteres")]
        public string Comentario { get; set; }
    }
}
