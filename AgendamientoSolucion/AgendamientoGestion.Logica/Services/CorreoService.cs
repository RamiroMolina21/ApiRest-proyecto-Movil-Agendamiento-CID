using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;

namespace AgendamientoGestion.Logica.Services
{
    public class CorreoService : ICorreoService
    {
        private readonly SmtpSettings _smtpSettings;

        public CorreoService(IOptions<SmtpSettings> smtpOptions)
        {
            _smtpSettings = smtpOptions.Value;
        }

        public void EnviarCorreo(string destinatario, string asunto, string htmlCuerpo, string rutaAdjunto = null)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(_smtpSettings.Remitente) ||
                    string.IsNullOrWhiteSpace(_smtpSettings.User) ||
                    string.IsNullOrWhiteSpace(_smtpSettings.Password) ||
                    string.IsNullOrWhiteSpace(destinatario) ||
                    string.IsNullOrWhiteSpace(asunto) ||
                    string.IsNullOrWhiteSpace(htmlCuerpo))
                {
                    throw new ArgumentException("Faltan datos necesarios para enviar el correo.");
                }

                var mensaje = new MailMessage(_smtpSettings.Remitente, destinatario, asunto, htmlCuerpo)
                {
                    IsBodyHtml = true
                };

                // Validar y adjuntar archivo
                if (!string.IsNullOrWhiteSpace(rutaAdjunto))
                {
                    if (File.Exists(rutaAdjunto))
                    {
                        mensaje.Attachments.Add(new Attachment(rutaAdjunto));
                    }
                    else
                    {
                        throw new FileNotFoundException("El archivo adjunto no se encontró.", rutaAdjunto);
                    }
                }

                using var cliente = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.User, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                cliente.Send(mensaje);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al enviar correo: {ex}");
                throw;
            }
        }

        public void EnviarConAdjunto(string destino, string asunto, string contenidoHtml, string rutaAdjunto)
        {
            Console.WriteLine("📤 Remitente que se usará: " + _smtpSettings.Remitente);

            if (string.IsNullOrWhiteSpace(_smtpSettings.Remitente))
                throw new Exception("El campo 'Remitente' está vacío.");

            var mensaje = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Remitente),
                Subject = asunto,
                Body = contenidoHtml,
                IsBodyHtml = true
            };

            mensaje.To.Add(destino);

            if (!string.IsNullOrWhiteSpace(rutaAdjunto) && File.Exists(rutaAdjunto))
            {
                mensaje.Attachments.Add(new Attachment(rutaAdjunto));
            }

            using var smtp = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                EnableSsl = _smtpSettings.EnableSsl,
                Credentials = new NetworkCredential(_smtpSettings.User, _smtpSettings.Password)
            };

            smtp.Send(mensaje);
        }
    }
}
