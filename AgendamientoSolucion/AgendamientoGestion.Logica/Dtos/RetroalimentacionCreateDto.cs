using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class RetroalimentacionCreateDto
    {
        [Required(ErrorMessage = "El comentario es obligatorio")]
        [StringLength(300, ErrorMessage = "El comentario no puede exceder 300 caracteres")]
        public string Comentario { get; set; }

        [Required(ErrorMessage = "La calificación es obligatoria")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public decimal Calificacion { get; set; }

        [Required(ErrorMessage = "El ID de la tutoría es obligatorio")]
        public int TutoriaId { get; set; }

        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }
    }
}
