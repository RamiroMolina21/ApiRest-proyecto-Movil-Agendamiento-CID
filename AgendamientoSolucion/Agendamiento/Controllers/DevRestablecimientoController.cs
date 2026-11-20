using Agendamiento.Security;
using AgendamientoGestion.Logica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Controllers
{
    /// <summary>
    /// SOLO Development (404 en cualquier otro entorno) y solo Administrador: devuelve un enlace de restablecimiento
    /// VÁLIDO para una cuenta "[PRUEBA]" (nombre que empieza por "[PRUEBA]", con acceso y activa) sin enviar correo.
    /// Para las e2e de la web contra la API real. Cuentas que no cumplen → 404 (no revela nada).
    /// </summary>
    [ApiController]
    [Route("dev/restablecimiento")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DevRestablecimientoController : ControllerBase
    {
        public sealed record EnlacePruebaDto(string? Correo);

        [HttpPost("enlace")]
        [Authorize(Policy = Politicas.SoloAdministrador)]
        public async Task<IActionResult> Enlace([FromBody] EnlacePruebaDto dto,
            [FromServices] IWebHostEnvironment entorno,
            [FromServices] IRestablecimientoContrasenaService servicio)
        {
            if (!entorno.IsDevelopment()) return NotFound();
            var enlace = await servicio.GenerarEnlacePruebaAsync(dto.Correo);
            if (enlace == null) return NotFound();
            var token = enlace[(enlace.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..];
            return Ok(new { enlace, token = Uri.UnescapeDataString(token) });
        }
    }
}