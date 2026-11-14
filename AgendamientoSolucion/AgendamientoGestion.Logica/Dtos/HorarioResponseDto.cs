using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class HorarioResponseDto
    {
        public int IdHorario { get; set; }
        public string Titulo { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime HoraInicio { get; set; }
        public DateTime HoraFin { get; set; }
        public string Cupos { get; set; }
        public string Espacio { get; set; }
        public string Estado { get; set; }
        /// <summary>Id del usuario dueño del horario (Horario.Usuario_idUsuario). null: el dueño fue eliminado (UsuarioNombre/Apellidos/Correo también null).</summary>
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? UsuarioApellidos { get; set; }
        public string? UsuarioCorreo { get; set; }
    }
}
