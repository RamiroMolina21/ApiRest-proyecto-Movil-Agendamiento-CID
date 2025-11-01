using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class EstudianteTutoriaDto
    {
        [Required(ErrorMessage = "El nombre del estudiante es obligatorio")]
        [StringLength(60, ErrorMessage = "El nombre no puede exceder 60 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Los apellidos del estudiante son obligatorios")]
        [StringLength(60, ErrorMessage = "Los apellidos no pueden exceder 60 caracteres")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "El correo del estudiante es obligatorio")]
        [StringLength(60, ErrorMessage = "El correo no puede exceder 60 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Correo { get; set; }
    }

    public class AgregarEstudiantesTutoriaDto
    {
        [Required(ErrorMessage = "Se requiere al menos un estudiante")]
        public List<EstudianteTutoriaDto> Estudiantes { get; set; }
    }
}

