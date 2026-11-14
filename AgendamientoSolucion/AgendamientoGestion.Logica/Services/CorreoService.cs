using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using System.Net.Sockets;

namespace AgendamientoGestion.Logica.Services
{
    /// <summary>
    /// Envío SMTP con MailKit (Microsoft desaconseja System.Net.Mail.SmtpClient para desarrollo nuevo y recomienda
    /// MailKit). Construye mensajes RFC 5322 correctos: multipart/alternative (texto + HTML) en UTF-8, From con nombre
    /// visible = cuenta autenticada, Reply-To opcional, Message-ID con el dominio del remitente, Date, y cabeceras de
    /// correo automático. TLS obligatorio si EnableSsl. Reintenta errores transitorios con espera creciente.
    /// </summary>
    public class CorreoService : ICorreoService
    {
        private static readonly TimeSpan[] Esperas = { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) };

        private readonly SmtpSettings _smtp;
        private readonly ILogger<CorreoService> _logger;

        public CorreoService(IOptions<SmtpSettings> smtpOptions, ILogger<CorreoService> logger)
        {
            _smtp = smtpOptions.Value;
            _logger = logger;
        }

        public string Enviar(MensajeCorreo mensaje)
        {
            ValidarConfiguracion(_smtp);
            using var mime = ConstruirMensaje(_smtp, mensaje);

            var intentos = Math.Max(0, _smtp.Reintentos) + 1;
            for (var intento = 1; ; intento++)
            {
                try
                {
                    using var cliente = new SmtpClient { Timeout = Math.Max(5, _smtp.TimeoutSegundos) * 1000 };
                    cliente.Connect(_smtp.Host, _smtp.Port, OpcionesTls(_smtp));
                    cliente.Authenticate(_smtp.User, _smtp.Password);
                    var respuesta = cliente.Send(mime);
                    cliente.Disconnect(true);

                    // Sin datos personales en el log: id del mensaje y dominio del destinatario.
                    _logger.LogInformation("Correo enviado {MessageId} a @{Dominio} (intento {Intento}). Respuesta SMTP: {Respuesta}",
                        mime.MessageId, Dominio(mensaje.Destinatario), intento, respuesta);
                    return mime.MessageId;
                }
                catch (Exception ex) when (EsTransitorio(ex) && intento < intentos)
                {
                    var espera = Esperas[Math.Min(intento - 1, Esperas.Length - 1)];
                    _logger.LogWarning(ex, "Fallo transitorio enviando {MessageId} (intento {Intento}/{Total}); reintento en {Espera}s",
                        mime.MessageId, intento, intentos, espera.TotalSeconds);
                    Thread.Sleep(espera);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo enviar el correo {MessageId} a @{Dominio} tras {Intento} intento(s)",
                        mime.MessageId, Dominio(mensaje.Destinatario), intento);
                    throw;
                }
            }
        }

        /// <summary>Falla ANTES de abrir conexión si falta configuración (SMTP sin configurar a propósito en desarrollo).</summary>
        public static void ValidarConfiguracion(SmtpSettings s)
        {
            if (string.IsNullOrWhiteSpace(s.Host) || string.IsNullOrWhiteSpace(s.User) ||
                string.IsNullOrWhiteSpace(s.Password) || string.IsNullOrWhiteSpace(s.DireccionRemitente))
                throw new InvalidOperationException("SMTP sin configurar: faltan SmtpSettings Host, User, Password o Remitente.");
        }

        /// <summary>Construye el MIME (público para poder verificar cabeceras y estructura en pruebas sin enviar).</summary>
        public static MimeMessage ConstruirMensaje(SmtpSettings s, MensajeCorreo m)
        {
            if (string.IsNullOrWhiteSpace(m.Destinatario)) throw new ArgumentException("Falta el destinatario", nameof(m));
            if (string.IsNullOrWhiteSpace(m.Asunto)) throw new ArgumentException("Falta el asunto", nameof(m));
            if (string.IsNullOrWhiteSpace(m.Html)) throw new ArgumentException("Falta el cuerpo", nameof(m));

            var desde = MailboxAddress.Parse(s.DireccionRemitente);
            desde.Name = string.IsNullOrWhiteSpace(s.NombreRemitente) ? null : s.NombreRemitente.Trim();

            var mensaje = new MimeMessage();
            mensaje.From.Add(desde);
            mensaje.To.Add(new MailboxAddress(m.NombreDestinatario?.Trim(), m.Destinatario.Trim()));
            if (!string.IsNullOrWhiteSpace(s.ResponderA))
                mensaje.ReplyTo.Add(MailboxAddress.Parse(s.ResponderA.Trim()));

            // Asunto en una línea (evita inyección de cabeceras con \r\n en datos dinámicos).
            mensaje.Subject = m.Asunto.Replace("\r", " ").Replace("\n", " ").Trim();
            mensaje.Date = DateTimeOffset.Now;
            mensaje.MessageId = MimeUtils.GenerateMessageId(desde.Domain);
            // Correo transaccional automático (RFC 3834): evita respuestas automáticas en bucle.
            mensaje.Headers.Add("Auto-Submitted", "auto-generated");
            mensaje.Headers.Add("X-Auto-Response-Suppress", "All");
            mensaje.Headers.Add(HeaderId.ContentLanguage, "es");

            var cuerpo = new BodyBuilder
            {
                TextBody = string.IsNullOrWhiteSpace(m.Texto) ? Correo.HtmlATexto.Convertir(m.Html) : m.Texto,
                HtmlBody = m.Html
            };
            if (!string.IsNullOrWhiteSpace(m.RutaAdjunto))
            {
                if (!File.Exists(m.RutaAdjunto)) throw new FileNotFoundException("El archivo adjunto no se encontró.", m.RutaAdjunto);
                cuerpo.Attachments.Add(m.RutaAdjunto);
            }
            mensaje.Body = cuerpo.ToMessageBody();
            foreach (var parte in mensaje.BodyParts.OfType<TextPart>())
                parte.ContentType.Charset = "utf-8";
            return mensaje;
        }

        private static SecureSocketOptions OpcionesTls(SmtpSettings s) =>
            !s.EnableSsl ? SecureSocketOptions.None
            : s.Port == 465 ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls; // falla si el servidor no ofrece STARTTLS: nunca se degrada a texto plano

        private static bool EsTransitorio(Exception ex) => ex switch
        {
            SmtpCommandException c => (int)c.StatusCode >= 400 && (int)c.StatusCode < 500,
            SmtpProtocolException or ServiceNotConnectedException => true,
            IOException or SocketException or TimeoutException => true,
            OperationCanceledException => true,
            _ => false
        };

        private static string Dominio(string correo)
        {
            var i = correo.LastIndexOf('@');
            return i >= 0 ? correo[(i + 1)..] : "?";
        }
    }
}
