using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EncuestaController : ControllerBase
    {
        private readonly IEncuestaService _encuestaService;

        public EncuestaController(IEncuestaService encuestaService)
        {
            _encuestaService = encuestaService;
        }

        // POST api/encuesta/enviar/{tutoriaId}
        // Llama cuando se finaliza una tutoría para enviar encuestas a los estudiantes
        [HttpPost("enviar/{tutoriaId}")]
        public async Task<IActionResult> EnviarEncuestas(int tutoriaId)
        {
            try
            {
                await _encuestaService.EnviarEncuestasAsync(tutoriaId);
                return Ok(new { message = "Encuestas enviadas correctamente a los estudiantes" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // GET api/encuesta/verificar/{token}
        // Valida que el token exista y no haya sido usado (llamado por React al cargar la página pública)
        [HttpGet("verificar/{token}")]
        public async Task<IActionResult> VerificarToken(string token)
        {
            try
            {
                var encuesta = await _encuestaService.VerificarTokenAsync(token);
                return Ok(encuesta);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // POST api/encuesta/responder
        // Endpoint público — el estudiante envía su respuesta sin login
        [HttpPost("responder")]
        public async Task<IActionResult> ResponderEncuesta([FromBody] EncuestaCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _encuestaService.ResponderEncuestaAsync(dto);
                return Ok(new { message = "¡Gracias por tu respuesta!", encuesta = resultado });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // GET api/encuesta/tutoria/{tutoriaId}
        // Ver todas las encuestas respondidas de una tutoría específica
        [HttpGet("tutoria/{tutoriaId}")]
        public async Task<IActionResult> GetByTutoria(int tutoriaId)
        {
            try
            {
                var encuestas = await _encuestaService.GetByTutoriaIdAsync(tutoriaId);
                return Ok(encuestas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        // GET api/encuesta/estadisticas
        // Para el Dashboard: promedio, total enviadas/respondidas, tasa de respuesta
        [HttpGet("estadisticas")]
        public async Task<IActionResult> GetEstadisticas()
        {
            try
            {
                var estadisticas = await _encuestaService.GetEstadisticasAsync();
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }
    }
}
