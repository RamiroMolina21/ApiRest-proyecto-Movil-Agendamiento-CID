using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class MetricasTutoriasDto
    {
        public DateTime Fecha { get; set; }
        public int TotalTutorias { get; set; }
        public int TutoriasCompletadas { get; set; }
        public int TutoriasCanceladas { get; set; }
        public decimal PromedioCalificacion { get; set; }
        public string Idioma { get; set; }
        public string Nivel { get; set; }
        public string Modalidad { get; set; }
    }
}
