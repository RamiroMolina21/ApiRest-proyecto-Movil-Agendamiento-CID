namespace AgendamientoGestion.Logica.Models
{
    /// <summary>
    /// Sección "SmtpSettings". User/Password en user-secrets (nunca en appsettings). Ver docs/correo/entregabilidad.md.
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// Dirección del From. Debe ser la cuenta autenticada (User) o un alias verificado en ese proveedor; si se
        /// deja vacía se usa User. Un From distinto de la cuenta que firma rompe la alineación SPF/DKIM (DMARC).
        /// </summary>
        public string Remitente { get; set; } = string.Empty;

        /// <summary>Nombre visible del remitente (solo identifica al emisor: sin asuntos ni emojis, guía de Google).</summary>
        public string NombreRemitente { get; set; } = "TutorPro – CECAR";

        /// <summary>Reply-To opcional (p. ej. el buzón de coordinación). Vacío: las respuestas van al Remitente.</summary>
        public string ResponderA { get; set; } = string.Empty;

        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        /// <summary>true: TLS obligatorio (STARTTLS en 587, TLS implícito en 465). Nunca se envía sin cifrar si es true.</summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>Tiempo máximo por intento de envío.</summary>
        public int TimeoutSegundos { get; set; } = 30;

        /// <summary>Reintentos ante errores transitorios (4xx SMTP, red). 0 = sin reintentos.</summary>
        public int Reintentos { get; set; } = 2;

        /// <summary>Dirección efectiva del From.</summary>
        public string DireccionRemitente => string.IsNullOrWhiteSpace(Remitente) ? User : Remitente.Trim();

        /// <summary>Dirección a la que el destinatario puede escribir (Reply-To si existe; si no, el From).</summary>
        public string? DireccionDeContacto
        {
            get
            {
                var d = string.IsNullOrWhiteSpace(ResponderA) ? DireccionRemitente : ResponderA.Trim();
                return string.IsNullOrWhiteSpace(d) ? null : d;
            }
        }
    }
}
