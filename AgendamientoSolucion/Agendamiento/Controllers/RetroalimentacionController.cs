using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RetroalimentacionController : ControllerBase
    {
        private readonly IRetroalimentacionService _retroalimentacionService;

        public RetroalimentacionController(IRetroalimentacionService retroalimentacionService)
        {
            _retroalimentacionService = retroalimentacionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRetroalimentacion([FromBody] RetroalimentacionCreateDto retroalimentacionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var retroalimentacion = await _retroalimentacionService.CreateRetroalimentacionAsync(retroalimentacionDto);
                return CreatedAtAction(nameof(GetRetroalimentacion), new { id = retroalimentacion.IdRetroalimentacion }, retroalimentacion);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRetroalimentacion(int id)
        {
            try
            {
                var retroalimentacion = await _retroalimentacionService.GetRetroalimentacionByIdAsync(id);
                return Ok(retroalimentacion);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRetroalimentaciones()
        {
            try
            {
                var retroalimentaciones = await _retroalimentacionService.GetAllRetroalimentacionesAsync();
                return Ok(retroalimentaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("tutoria/{tutoriaId}")]
        public async Task<IActionResult> GetRetroalimentacionesByTutoria(int tutoriaId)
        {
            try
            {
                var retroalimentaciones = await _retroalimentacionService.GetRetroalimentacionesByTutoriaAsync(tutoriaId);
                return Ok(retroalimentaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> GetRetroalimentacionesByUsuario(int usuarioId)
        {
            try
            {
                var retroalimentaciones = await _retroalimentacionService.GetRetroalimentacionesByUsuarioAsync(usuarioId);
                return Ok(retroalimentaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRetroalimentacion(int id, [FromBody] RetroalimentacionCreateDto retroalimentacionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var retroalimentacion = await _retroalimentacionService.UpdateRetroalimentacionAsync(id, retroalimentacionDto);
                return Ok(retroalimentacion);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRetroalimentacion(int id)
        {
            try
            {
                var result = await _retroalimentacionService.DeleteRetroalimentacionAsync(id);
                if (result)
                {
                    return Ok(new { message = "Retroalimentación eliminada exitosamente" });
                }
                return NotFound(new { message = "Retroalimentación no encontrada" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}
