using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class TutoriaResponseDto
    {
        public int IdTutoria { get; set; }
        public string Idioma { get; set; }
        public string Nivel { get; set; }
        public string Tema { get; set; }
        public string Modalidad { get; set; }
        public string Estado { get; set; }
        public DateTime FechaTutoria { get; set; }
        public string UsuarioNombre { get; set; }
        public string UsuarioApellidos { get; set; }
        public string UsuarioCorreo { get; set; }
        public string HorarioEspacio { get; set; }
        public DateTime HorarioHoraInicio { get; set; }
        public DateTime HorarioHoraFin { get; set; }
        public List<UsuarioResponseDto> Estudiantes { get; set; }
    }
}
