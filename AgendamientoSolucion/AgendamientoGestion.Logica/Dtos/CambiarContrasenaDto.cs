using System.ComponentModel.DataAnnotations;

namespace AgendamientoGestion.Logica.Dtos;

/// <summary>Cuerpo de PUT /api/Auth/contrasena (el usuario cambia SU propia contraseña).</summary>
public class CambiarContrasenaDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria")]
    public string ContrasenaActual { get; set; } = string.Empty;

    /// <summary>
    /// 8–72 caracteres (BCrypt solo usa los primeros 72 bytes; se valida también en bytes UTF-8).
    /// Sin reglas de composición (criterio NIST SP 800-63B: longitud antes que complejidad).
    /// </summary>
    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "La nueva contraseña debe tener entre 8 y 72 caracteres")]
    public string ContrasenaNueva { get; set; } = string.Empty;
}
