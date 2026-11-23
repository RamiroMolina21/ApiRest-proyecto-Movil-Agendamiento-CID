namespace AgendamientoGestion.Logica.Models
{
    /// <summary>
    /// Proveedor de correo (sección "Correo"). "Smtp" (por defecto) o "GmailApi" (HTTPS 443: para hosts que bloquean
    /// los puertos SMTP, como los servicios web gratuitos de Render). Ver docs/correo/gmail-api.md.
    /// </summary>
    public class CorreoSettings
    {
        public const string SectionName = "Correo";
        public const string ProveedorSmtp = "Smtp";
        public const string ProveedorGmailApi = "GmailApi";

        public string Proveedor { get; set; } = ProveedorSmtp;

        public bool UsaGmailApi => string.Equals(Proveedor?.Trim(), ProveedorGmailApi, StringComparison.OrdinalIgnoreCase);

        /// <summary>Devuelve el proveedor normalizado o lanza con un mensaje claro si el valor no es válido.</summary>
        public string ProveedorValidado()
        {
            var p = string.IsNullOrWhiteSpace(Proveedor) ? ProveedorSmtp : Proveedor.Trim();
            if (string.Equals(p, ProveedorSmtp, StringComparison.OrdinalIgnoreCase)) return ProveedorSmtp;
            if (string.Equals(p, ProveedorGmailApi, StringComparison.OrdinalIgnoreCase)) return ProveedorGmailApi;
            throw new InvalidOperationException(
                $"Correo:Proveedor = '{p}' no es válido. Valores admitidos: '{ProveedorSmtp}' o '{ProveedorGmailApi}'.");
        }
    }

    /// <summary>
    /// Sección "GmailApi": OAuth2 con refresh token (alcance gmail.send). ClientSecret y RefreshToken son SECRETOS:
    /// user-secrets en local y variables de entorno en Render (GmailApi__ClientId, GmailApi__ClientSecret,
    /// GmailApi__RefreshToken). Nunca en appsettings ni en logs. El From sale de SmtpSettings:Remitente (o User).
    /// </summary>
    public class GmailApiSettings
    {
        public const string SectionName = "GmailApi";

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        public string TokenUrl { get; set; } = "https://oauth2.googleapis.com/token";
        public string EnviarUrl { get; set; } = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

        /// <summary>Tiempo máximo por petición HTTP.</summary>
        public int TimeoutSegundos { get; set; } = 30;

        /// <summary>Reintentos ante errores transitorios (429, 5xx, red). 0 = sin reintentos.</summary>
        public int Reintentos { get; set; } = 2;

        /// <summary>Espera del primer reintento (luego x2.5 y x5, como el SMTP: 2 s, 5 s, 10 s).</summary>
        public int EsperaBaseMilisegundos { get; set; } = 2000;
    }
}
