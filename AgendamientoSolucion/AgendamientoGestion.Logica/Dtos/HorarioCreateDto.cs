using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class HorarioCreateDto
    {
        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        public DateTime FechaFin { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        public DateTime HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        public DateTime HoraFin { get; set; }

        [Required(ErrorMessage = "Los cupos son obligatorios")]
        [StringLength(60, ErrorMessage = "Los cupos no pueden exceder 60 caracteres")]
        public string Cupos { get; set; }

        [Required(ErrorMessage = "El espacio es obligatorio")]
        [StringLength(60, ErrorMessage = "El espacio no puede exceder 60 caracteres")]
        public string Espacio { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(60, ErrorMessage = "El estado no puede exceder 60 caracteres")]
        public string Estado { get; set; }

        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }
    }
}
