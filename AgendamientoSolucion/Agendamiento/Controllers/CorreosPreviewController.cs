using AgendamientoGestion.Logica.Correo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace Agendamiento.Controllers
{
    /// <summary>
    /// SOLO en Development (en cualquier otro entorno responde 404): previsualiza las plantillas de correo con
    /// datos de ejemplo (EjemplosCorreo), sin enviar nada. Para diseñar: http://localhost:5208/dev/correos
    /// Anónimo a propósito (solo datos ficticios) para poder abrirlo directamente en el navegador.
    /// </summary>
    [ApiController]
    [Route("dev/correos")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CorreosPreviewController : ControllerBase
    {
        private readonly IPlantillasCorreo _plantillas;
        private readonly IWebHostEnvironment _entorno;

        public CorreosPreviewController(IPlantillasCorreo plantillas, IWebHostEnvironment entorno)
        {
            _plantillas = plantillas;
            _entorno = entorno;
        }

        [HttpGet]
        [AllowAnonymous] // previsualización: solo datos ficticios
        public IActionResult Indice()
        {
            if (!_entorno.IsDevelopment()) return NotFound();
            var sb = new StringBuilder("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Correos</title></head><body style=\"font-family:Arial,sans-serif\"><h1>Plantillas de correo (ejemplos)</h1><ul>");
            foreach (var m in EjemplosCorreo.Todos())
            {
                var p = WebUtility.HtmlEncode(m.Plantilla);
                sb.Append($"<li><strong>{p}</strong> — {WebUtility.HtmlEncode(m.Asunto)} · <a href=\"correos/{p}\">HTML</a> · <a href=\"correos/{p}?formato=texto\">texto</a></li>");
            }
            sb.Append("</ul></body></html>");
            return Content(sb.ToString(), "text/html; charset=utf-8");
        }

        public sealed record EnvioPruebaDto(string? Para);

        /// <summary>
        /// Envía UN correo REAL de ejemplo (para probar en un buzón de verdad). Solo Development, solo Administrador y
        /// solo a direcciones de la lista blanca CorreoPruebas:DestinatariosPermitidos (vacía por defecto → 403).
        /// </summary>
        [HttpPost("{plantilla}/enviar")]
        [Authorize(Policy = Agendamiento.Security.Politicas.SoloAdministrador)]
        public IActionResult EnviarPrueba(string plantilla, [FromBody] EnvioPruebaDto dto,
            [FromServices] AgendamientoGestion.Logica.Interfaces.ICorreoService correo,
            [FromServices] IConfiguration configuracion,
            [FromServices] Microsoft.Extensions.Options.IOptions<AgendamientoGestion.Logica.Models.SmtpSettings> smtp)
        {
            if (!_entorno.IsDevelopment()) return NotFound();
            var modelo = EjemplosCorreo.Buscar(plantilla);
            if (modelo == null) return NotFound();

            var permitidos = configuracion.GetSection("CorreoPruebas:DestinatariosPermitidos").Get<string[]>() ?? Array.Empty<string>();
            var para = dto.Para?.Trim();
            if (string.IsNullOrEmpty(para) || !permitidos.Contains(para, StringComparer.OrdinalIgnoreCase))
                return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Destinatario no permitido (CorreoPruebas:DestinatariosPermitidos)");

            var c = _plantillas.Renderizar(modelo);
            var messageId = correo.Enviar(AgendamientoGestion.Logica.Interfaces.MensajeCorreo.Desde(c, para));
            var s = smtp.Value;
            return Ok(new
            {
                messageId,
                from = $"{s.NombreRemitente} <{s.DireccionRemitente}>",
                replyTo = string.IsNullOrWhiteSpace(s.ResponderA) ? null : s.ResponderA,
                subject = c.Asunto,
                partes = new[] { "text/plain; charset=utf-8", "text/html; charset=utf-8" }
            });
        }

        [HttpGet("{plantilla}")]
        [AllowAnonymous]
        public IActionResult Ver(string plantilla, [FromQuery] string? formato)
        {
            if (!_entorno.IsDevelopment()) return NotFound();
            var modelo = EjemplosCorreo.Buscar(plantilla);
            if (modelo == null) return NotFound();
            var correo = _plantillas.Renderizar(modelo);
            return string.Equals(formato, "texto", StringComparison.OrdinalIgnoreCase)
                ? Content($"Asunto: {correo.Asunto}\r\n\r\n{correo.Texto}", "text/plain; charset=utf-8")
                : Content(correo.Html, "text/html; charset=utf-8");
        }
    }
}
