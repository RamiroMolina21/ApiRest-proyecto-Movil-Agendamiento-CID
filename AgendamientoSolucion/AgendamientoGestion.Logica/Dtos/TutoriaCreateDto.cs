using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class TutoriaCreateDto
    {
        [Required(ErrorMessage = "El idioma es obligatorio")]
        [StringLength(45, ErrorMessage = "El idioma no puede exceder 45 caracteres")]
        public string Idioma { get; set; }

        [Required(ErrorMessage = "El nivel es obligatorio")]
        [StringLength(45, ErrorMessage = "El nivel no puede exceder 45 caracteres")]
        public string Nivel { get; set; }

        [Required(ErrorMessage = "El tema es obligatorio")]
        [StringLength(45, ErrorMessage = "El tema no puede exceder 45 caracteres")]
        public string Tema { get; set; }

        [Required(ErrorMessage = "La modalidad es obligatoria")]
        [StringLength(45, ErrorMessage = "La modalidad no puede exceder 45 caracteres")]
        public string Modalidad { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(45, ErrorMessage = "El estado no puede exceder 45 caracteres")]
        public string Estado { get; set; }

        [Required(ErrorMessage = "La fecha de la tutoría es obligatoria")]
        public DateTime FechaTutoria { get; set; }

        [Required(ErrorMessage = "El nombre del tutor es obligatorio")]
        [StringLength(60, ErrorMessage = "El nombre no puede exceder 60 caracteres")]
        public string TutorNombre { get; set; }

        [Required(ErrorMessage = "Los apellidos del tutor son obligatorios")]
        [StringLength(60, ErrorMessage = "Los apellidos no pueden exceder 60 caracteres")]
        public string TutorApellidos { get; set; }

        [Required(ErrorMessage = "El correo del tutor es obligatorio")]
        [StringLength(60, ErrorMessage = "El correo no puede exceder 60 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string TutorCorreo { get; set; }

        [Required(ErrorMessage = "El ID del horario es obligatorio")]
        public int HorarioId { get; set; }
    }
}
