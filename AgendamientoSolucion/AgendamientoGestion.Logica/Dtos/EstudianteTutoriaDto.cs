using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class AgregarEstudiantesTutoriaDto
    {
        [Required(ErrorMessage = "Se requiere al menos un ID de estudiante")]
        [MinLength(1, ErrorMessage = "Debe incluir al menos un ID de estudiante")]
        public List<int> EstudianteIds { get; set; }
    }
}

