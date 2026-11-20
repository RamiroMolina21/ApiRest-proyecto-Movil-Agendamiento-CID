namespace Agendamiento.Security
{
    /// <summary>Nombres de las políticas de rate limiting por IP del restablecimiento de contraseña.</summary>
    public static class RateLimitRestablecimiento
    {
        public const string PoliticaSolicitud = "olvide-contrasena";
        public const string PoliticaRestablecer = "restablecer-contrasena";
    }
}