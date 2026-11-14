using System.Text;

namespace Agendamiento.Security
{
    /// <summary>
    /// Sección "Jwt" de configuración. La clave NUNCA va en appsettings.json:
    ///   desarrollo → dotnet user-secrets set "Jwt:Key" "&lt;clave aleatoria de 32+ bytes&gt;"
    ///   producción → variable de entorno Jwt__Key
    /// </summary>
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>HS256 exige una clave de al menos 256 bits.</summary>
        public const int MinKeyBytes = 32;

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "AgendamientoApi";
        public string Audience { get; set; } = "AgendamientoClientes";

        /// <summary>Vida del token de acceso. Por defecto 8 h (una jornada).</summary>
        public int ExpirationMinutes { get; set; } = 480;

        public byte[] KeyBytes => Encoding.UTF8.GetBytes(Key ?? string.Empty);

        /// <summary>Devuelve null si es válida o el mensaje de error que se mostrará al arrancar.</summary>
        public string? Validar()
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                return "Falta la configuración 'Jwt:Key'. En desarrollo ejecute en la carpeta Agendamiento: " +
                       "dotnet user-secrets set \"Jwt:Key\" \"<clave aleatoria de al menos 32 caracteres>\". " +
                       "En producción defina la variable de entorno Jwt__Key.";
            }

            if (KeyBytes.Length < MinKeyBytes)
            {
                return $"'Jwt:Key' es demasiado corta ({KeyBytes.Length} bytes). HS256 requiere al menos {MinKeyBytes} bytes.";
            }

            if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            {
                return "'Jwt:Issuer' y 'Jwt:Audience' son obligatorios.";
            }

            if (ExpirationMinutes <= 0 || ExpirationMinutes > 24 * 60)
            {
                return "'Jwt:ExpirationMinutes' debe estar entre 1 y 1440.";
            }

            return null;
        }
    }
}
