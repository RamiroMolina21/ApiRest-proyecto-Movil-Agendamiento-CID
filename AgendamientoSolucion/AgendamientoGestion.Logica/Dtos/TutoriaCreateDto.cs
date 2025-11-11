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

        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El ID del horario es obligatorio")]
        public int HorarioId { get; set; }
    }
}
