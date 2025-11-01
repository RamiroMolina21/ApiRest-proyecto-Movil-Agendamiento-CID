using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class UsuarioCreateDto
    {
        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [StringLength(60, ErrorMessage = "Los nombres no pueden exceder 60 caracteres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(60, ErrorMessage = "Los apellidos no pueden exceder 60 caracteres")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [StringLength(60, ErrorMessage = "El correo no puede exceder 60 caracteres")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        public int RolId { get; set; }
    }
}
