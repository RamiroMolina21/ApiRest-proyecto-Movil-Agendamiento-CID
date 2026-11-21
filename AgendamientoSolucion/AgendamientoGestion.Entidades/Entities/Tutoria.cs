using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Entidades.Entities
{
    public class Tutoria {
        public int idTutoria { get; set; }
        public string idioma { get; set; }
        public string nivel { get; set; }
        public string tema { get; set; }
        public string modalidad { get; set; }
        public string estado { get; set; }
        public DateTime fechaHora { get; set; }
        /// <summary>Asignado (Docente/Tutor). null: el usuario fue eliminado; la tutoría queda sin asignar.</summary>
        public int? Usuario_idUsuario { get; set; }
        public int Horario_idHorario { get; set; }
        /// <summary>Máximo de estudiantes (1..1000). null: tutoría anterior al cambio → se usan los cupos del horario.</summary>
        public int? cupos { get; set; }

        // Propiedades de navegación
        public Usuario? Usuario { get; set; }
        public Horario Horario { get; set; }
        public ICollection<Informe> Informes { get; set; }
        public ICollection<Retroalimentacion> Retroalimentaciones { get; set; }
        public ICollection<TutoriaEstudiante> TutoriaEstudiantes { get; set; }
        public ICollection<EncuestaSatisfaccion> EncuestasSatisfaccion { get; set; }
    }
}
