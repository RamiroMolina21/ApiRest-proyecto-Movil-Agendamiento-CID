using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class MetricasResponseDto
    {
        public int TotalTutorias { get; set; }
        public int TutoriasCompletadas { get; set; }
        public int TutoriasCanceladas { get; set; }
        public int TutoriasProgramadas { get; set; }
        public decimal PromedioCalificacion { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalDocentes { get; set; }
        public int TotalEstudiantes { get; set; }
        public int TotalHorarios { get; set; }
        public int HorariosDisponibles { get; set; }
    }
}
