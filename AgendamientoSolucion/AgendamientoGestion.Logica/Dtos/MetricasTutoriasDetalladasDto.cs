using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class MetricasTutoriasDetalladasDto
    {
        // Datos de la tutoría
        public int IdTutoria { get; set; }
        public string Idioma { get; set; }
        public string Nivel { get; set; }
        public string Tema { get; set; }
        public string Modalidad { get; set; }
        public string Estado { get; set; }
        public DateTime FechaTutoria { get; set; }
        public string HorarioEspacio { get; set; }
        public DateTime HorarioHoraInicio { get; set; }
        public DateTime HorarioHoraFin { get; set; }

        // Datos del tutor/profesor
        public int TutorId { get; set; }
        public string TutorNombre { get; set; }
        public string TutorApellidos { get; set; }
        public string TutorCorreo { get; set; }

        // Datos de estudiantes y asistencias
        public int NumeroAsistencias { get; set; }
        public int TotalEstudiantesAsignados { get; set; }
        public List<EstudianteMetricasDto> Estudiantes { get; set; }
    }

    public class EstudianteMetricasDto
    {
        public int EstudianteId { get; set; }
        public string EstudianteNombre { get; set; }
        public string EstudianteApellidos { get; set; }
        public string EstudianteCorreo { get; set; }
        public bool Asistio { get; set; }
    }
}

