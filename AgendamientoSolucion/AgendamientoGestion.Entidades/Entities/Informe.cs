using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Entidades.Entities
{
    public class Informe {
        public int idInforme { get; set; }
        public string descripcion { get; set; }
        public DateTime fechaGeneracion { get; set; }
        public int Tutoria_idTutoria { get; set; }
        public int Usuario_idUsuario { get; set; }

        // Propiedades de navegación
        public Tutoria Tutoria { get; set; }
        public Usuario Usuario { get; set; }
    }
}
