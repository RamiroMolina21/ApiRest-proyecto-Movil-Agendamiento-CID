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
        /// <summary>Id del docente/tutor asignado (Tutoria.Usuario_idUsuario). null: sin asignar porque el usuario fue eliminado (nombre/apellidos/correo también null).</summary>
        public int? UsuarioId { get; set; }
        /// <summary>Id del horario (Tutoria.Horario_idHorario). Aditivo.</summary>
        public int HorarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? UsuarioApellidos { get; set; }
        public string? UsuarioCorreo { get; set; }
        public string HorarioEspacio { get; set; }
        public DateTime HorarioHoraInicio { get; set; }
        public DateTime HorarioHoraFin { get; set; }
        /// <summary>Dueño del horario (Horario.Usuario_idUsuario): el Docente que agendó. null: usuario eliminado (nombre/apellidos también null).</summary>
        public int? HorarioUsuarioId { get; set; }
        public string? HorarioUsuarioNombre { get; set; }
        public string? HorarioUsuarioApellidos { get; set; }
        public string HorarioTitulo { get; set; }
        /// <summary>Máximo de estudiantes; null si el valor guardado no es un entero ≥ 1 (sin límite). Aditivo.</summary>
        public int? HorarioCupos { get; set; }
        /// <summary>
        /// Capacidad EFECTIVA de la tutoría: Tutoria.cupos; si es null (tutorías antiguas) los del horario;
        /// null = sin límite. Es el campo que debe usar la UI. Aditivo (2026-11-21).
        /// </summary>
        public int? Cupos { get; set; }
        public List<UsuarioResponseDto> Estudiantes { get; set; } = new();
    }
}
