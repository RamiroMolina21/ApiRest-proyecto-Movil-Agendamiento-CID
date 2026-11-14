using Agendamiento.Security;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EncuestaController : ControllerBase
    {
        private readonly IEncuestaService _encuestaService;

        private readonly ITutoriaService _tutoriaService;

        public EncuestaController(IEncuestaService encuestaService, ITutoriaService tutoriaService)
        {
            _encuestaService = encuestaService;
            _tutoriaService = tutoriaService;
        }

        // POST api/encuesta/enviar/{tutoriaId}
        // Idempotente: PUT /Tutoria/{id}/finalizar ya las envía; repetir la llamada no duplica encuestas.
        [HttpPost("enviar/{tutoriaId}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> EnviarEncuestas(int tutoriaId)
        {
            // Misma regla de propiedad que finalizar (Docente participante / Tutor asignado / Admin-Aux).
            await _tutoriaService.AsegurarAccesoAsync(tutoriaId, User.ComoSolicitante(), AccionTutoria.Finalizar);
            var resultado = await _encuestaService.EnviarEncuestasAsync(tutoriaId);
            return Ok(new
            {
                message = "Encuestas enviadas correctamente a los estudiantes",
                enviadas = resultado.Enviadas,
                yaExistentes = resultado.YaExistentes
            });
        }

        // GET api/encuesta/verificar/{token} — PÚBLICO (página de encuesta de la web, sin login)
        [HttpGet("verificar/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarToken(string token)
        {
            return Ok(await _encuestaService.VerificarTokenAsync(token));
        }

        // POST api/encuesta/responder — PÚBLICO (el token de un solo uso autoriza la respuesta)
        [HttpPost("responder")]
        [AllowAnonymous]
        public async Task<IActionResult> ResponderEncuesta([FromBody] EncuestaCreateDto dto)
        {
            var resultado = await _encuestaService.ResponderEncuestaAsync(dto);
            return Ok(new { message = "¡Gracias por tu respuesta!", encuesta = resultado });
        }

        // GET api/encuesta/tutoria/{tutoriaId}
        [HttpGet("tutoria/{tutoriaId}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetByTutoria(int tutoriaId)
        {
            return Ok(await _encuestaService.GetByTutoriaIdAsync(tutoriaId));
        }

        // GET api/encuesta/estadisticas
        [HttpGet("estadisticas")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetEstadisticas()
        {
            return Ok(await _encuestaService.GetEstadisticasAsync());
        }

        // GET api/encuesta/pendientes — tutorías con encuestas sin responder (dashboard: "encuestas-sin-responder")
        [HttpGet("pendientes")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetPendientes()
        {
            return Ok(await _encuestaService.GetPendientesAsync());
        }
    }
}
