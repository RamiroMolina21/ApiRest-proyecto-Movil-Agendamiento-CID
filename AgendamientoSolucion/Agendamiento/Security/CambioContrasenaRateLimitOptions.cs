namespace Agendamiento.Security
{
    /// <summary>
    /// Sección "RateLimiting:CambioContrasena". Intentos de cambio de contraseña por usuario (claim "sub";
    /// IP si no hay token) en una ventana fija. Limita la adivinación de la contraseña actual con un token robado.
    /// </summary>
    public sealed class CambioContrasenaRateLimitOptions
    {
        public const string SectionName = "RateLimiting:CambioContrasena";
        public const string PolicyName = "cambio-contrasena";

        public int PermitLimit { get; set; } = 5;
        public int WindowSeconds { get; set; } = 900;
    }
}
