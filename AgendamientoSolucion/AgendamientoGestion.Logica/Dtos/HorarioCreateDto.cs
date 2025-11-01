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

        [Required(ErrorMessage = "El nombre del docente es obligatorio")]
        [StringLength(60, ErrorMessage = "El nombre no puede exceder 60 caracteres")]
        public string DocenteNombre { get; set; }

        [Required(ErrorMessage = "Los apellidos del docente son obligatorios")]
        [StringLength(60, ErrorMessage = "Los apellidos no pueden exceder 60 caracteres")]
        public string DocenteApellidos { get; set; }

        [Required(ErrorMessage = "El correo del docente es obligatorio")]
        [StringLength(60, ErrorMessage = "El correo no puede exceder 60 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string DocenteCorreo { get; set; }
    }
}
