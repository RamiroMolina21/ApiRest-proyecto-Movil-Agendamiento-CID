using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Entidades.Entities
{
    public class Rol {
        public int idRol { get; set; }
        public string tipoRol { get; set; }

        // Propiedades de navegación
        public ICollection<Usuario> Usuarios { get; set; }
    }
}
