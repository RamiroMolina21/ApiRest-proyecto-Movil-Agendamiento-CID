using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Entidades.Entities
{
    public class Usuario {
        public int idUsuario { get; set; }
        public string nombres { get; set; }
        public string apellidos { get; set; }
        public string correo { get; set; }
        public string contrasenaHash { get; set; }
        public DateTime fechaRegistro { get; set; }
        public int Rol_idRol { get; set; }

        // Propiedades de navegación
        public Rol Rol { get; set; }
        public ICollection<Horario> Horarios { get; set; }
        public ICollection<Tutoria> Tutorias { get; set; }
        public ICollection<Notificacion> Notificaciones { get; set; }
        public ICollection<Informe> Informes { get; set; }
        public ICollection<Retroalimentacion> Retroalimentaciones { get; set; }
        public ICollection<TutoriaEstudiante> TutoriaEstudiantes { get; set; }
    }
}
