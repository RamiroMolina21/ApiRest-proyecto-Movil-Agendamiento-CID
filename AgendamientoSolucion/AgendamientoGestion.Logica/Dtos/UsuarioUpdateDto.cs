using System.ComponentModel.DataAnnotations;

namespace AgendamientoGestion.Logica.Dtos
{
    /// <summary>
    /// Cuerpo de PUT /api/Usuario/{id}. Igual que <see cref="UsuarioCreateDto"/> pero la contraseña es opcional:
    /// si se omite, es null o "", se conserva la actual (antes era obligatoria y siempre se re-hasheaba).
    /// </summary>
    public class UsuarioUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [StringLength(60, ErrorMessage = "Los nombres no pueden exceder 60 caracteres")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(60, ErrorMessage = "Los apellidos no pueden exceder 60 caracteres")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [StringLength(60, ErrorMessage = "El correo no puede exceder 60 caracteres")]
        public string Correo { get; set; } = string.Empty;

        /// <summary>Opcional. Si se envía con contenido, mínimo 6 caracteres.</summary>
        [StringLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
        public string? Contrasena { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        public int RolId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Contrasena) && Contrasena.Length < 6)
            {
                yield return new ValidationResult(
                    "La contraseña debe tener al menos 6 caracteres",
                    new[] { nameof(Contrasena) });
            }
        }
    }
}
