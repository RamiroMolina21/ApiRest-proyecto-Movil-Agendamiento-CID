using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class NotificacionResponseDto
    {
        public int IdNotificacion { get; set; }
        public DateTime Fecha { get; set; }
        public string Asunto { get; set; }
        public string Descripcion { get; set; }
        public string UsuarioNombre { get; set; }
        public string UsuarioApellidos { get; set; }
        public string UsuarioCorreo { get; set; }
    }
}
