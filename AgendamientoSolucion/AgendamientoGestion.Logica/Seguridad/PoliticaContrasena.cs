using AgendamientoGestion.Logica.Exceptions;

namespace AgendamientoGestion.Logica.Seguridad;

/// <summary>
/// Reglas de la contraseña nueva, compartidas por PUT /Auth/contrasena y POST /Auth/restablecer-contrasena
/// (mismas reglas, mismo orden, mismos mensajes). Sin reglas de composición (NIST SP 800-63B).
/// </summary>
public static class PoliticaContrasena
{
    public const string MensajeLongitud = "La nueva contraseña debe tener entre 8 y 72 caracteres (máximo 72 bytes)";
    public const string MensajeSoloEspacios = "La nueva contraseña no puede estar formada solo por espacios";
    public const string MensajeIgualActual = "La nueva contraseña debe ser distinta de la actual";
    public const string MensajeIgualCorreo = "La nueva contraseña no puede ser igual a su correo";

    /// <param name="esIgualALaActual">Cómo saber si coincide con la actual (texto en el cambio; BCrypt en el restablecimiento).</param>
    public static void Validar(string? nueva, string? correo, Func<string, bool> esIgualALaActual)
    {
        nueva ??= string.Empty;
        if (nueva.Length < 8 || System.Text.Encoding.UTF8.GetByteCount(nueva) > 72)
            throw new ValidationException(MensajeLongitud);
        if (string.IsNullOrWhiteSpace(nueva))
            throw new ValidationException(MensajeSoloEspacios);
        if (esIgualALaActual(nueva))
            throw new ValidationException(MensajeIgualActual);
        if (string.Equals(nueva.Trim(), correo?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(MensajeIgualCorreo);
    }
}