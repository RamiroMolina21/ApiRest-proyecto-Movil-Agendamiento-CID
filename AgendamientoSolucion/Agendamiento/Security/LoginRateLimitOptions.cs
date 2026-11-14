namespace Agendamiento.Security
{
    /// <summary>Sección "RateLimiting:Login". Límite de intentos de login por IP en una ventana fija.</summary>
    public sealed class LoginRateLimitOptions
    {
        public const string SectionName = "RateLimiting:Login";
        public const string PolicyName = "login";

        public int PermitLimit { get; set; } = 10;
        public int WindowSeconds { get; set; } = 60;
    }
}
