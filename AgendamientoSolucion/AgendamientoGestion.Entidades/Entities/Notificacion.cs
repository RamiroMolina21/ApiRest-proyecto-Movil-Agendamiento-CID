using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Entidades.Entities
{
    public class Notificacion {
        public int idNotificacion { get; set; }
        public DateTime fecha { get; set; }
        public string asunto { get; set; }
        public string descripcion { get; set; }
        public int Usuario_idUsuario { get; set; }

        // Propiedades de navegación
        public Usuario Usuario { get; set; }
    }
}
