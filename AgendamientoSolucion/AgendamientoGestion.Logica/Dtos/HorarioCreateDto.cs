using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class HorarioCreateDto
    {
        /// <summary>
        /// Opcional. Omitido, null o vacío → el servidor genera uno (ver TituloHorario). En PUT, vacío conserva
        /// el título personalizado existente o regenera el generado automáticamente.
        /// </summary>
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        public string? Titulo { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        public DateTime FechaFin { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        public DateTime HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        public DateTime HoraFin { get; set; }

        /// <summary>
        /// OPCIONAL desde 2026-11-21 (la capacidad se define en la tutoría). null, ausente o vacío → se guarda "";
        /// si viene, entero 1..1000.
        /// </summary>
        [StringLength(60, ErrorMessage = "Los cupos no pueden exceder 60 caracteres")]
        public string? Cupos { get; set; }

        /// <summary>Aula. Opcional: null, ausente, "" o solo espacios = sin aula (se guarda ""). Máximo 60.</summary>
        [StringLength(60, ErrorMessage = "El espacio no puede exceder 60 caracteres")]
        public string? Espacio { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(60, ErrorMessage = "El estado no puede exceder 60 caracteres")]
        public string Estado { get; set; }

        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }
    }
}
