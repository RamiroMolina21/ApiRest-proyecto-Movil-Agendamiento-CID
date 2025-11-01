using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class NotificacionCreateDto
    {
        [Required(ErrorMessage = "El asunto es obligatorio")]
        [StringLength(45, ErrorMessage = "El asunto no puede exceder 45 caracteres")]
        public string Asunto { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(250, ErrorMessage = "La descripción no puede exceder 250 caracteres")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }
    }
}
