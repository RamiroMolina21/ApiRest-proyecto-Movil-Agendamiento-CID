namespace Agendamiento.Security
{
    /// <summary>
    /// Política CORS única. Los orígenes permitidos se leen de "Cors:AllowedOrigins" (array).
    /// La app MAUI no usa CORS (no es un navegador); solo aplica a la web React.
    /// </summary>
    public static class CorsConfig
    {
        public const string PolicyName = "Clientes";
    }
}
