using AgendamientoGestion.Logica.Exceptions;
using System.Text.RegularExpressions;

namespace AgendamientoGestion.Logica.Utils
{
    public static class EmailValidator
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(250));

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Lanza <see cref="ValidationException"/> (HTTP 400) si el correo es vacío o inválido.
        /// Antes lanzaba ArgumentException, que terminaba como HTTP 500.
        /// </summary>
        public static void ValidateEmail(string? email, string fieldName = "Correo")
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException($"{fieldName} es requerido");

            if (!IsValidEmail(email))
                throw new ValidationException($"{fieldName} no tiene un formato válido");
        }
    }
}
