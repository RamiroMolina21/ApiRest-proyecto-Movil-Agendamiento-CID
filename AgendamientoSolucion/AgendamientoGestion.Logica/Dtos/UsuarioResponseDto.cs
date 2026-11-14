using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class UsuarioResponseDto
    {
        public int IdUsuario { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string TipoRol { get; set; }
        /// <summary>Id del rol (Usuario.Rol_idRol). Aditivo.</summary>
        public int RolId { get; set; }
        /// <summary>Cuenta habilitada. false: no puede iniciar sesión ni ser asignado a tutorías. Aditivo.</summary>
        public bool Activo { get; set; }
    }
}
