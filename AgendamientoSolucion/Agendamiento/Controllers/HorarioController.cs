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
    public class HorarioController : ControllerBase
    {
        private readonly IHorarioServices _horarioService;

        public HorarioController(IHorarioServices horarioService)
        {
            _horarioService = horarioService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateHorario([FromBody] HorarioCreateDto horarioDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var horario = await _horarioService.CreateHorarioAsync(horarioDto);
                return CreatedAtAction(nameof(GetHorario), new { id = horario.IdHorario }, horario);
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
        public async Task<IActionResult> GetHorario(int id)
        {
            try
            {
                var horario = await _horarioService.GetHorarioByIdAsync(id);
                return Ok(horario);
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
        public async Task<IActionResult> GetAllHorarios()
        {
            try
            {
                var horarios = await _horarioService.GetAllHorariosAsync();
                return Ok(horarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("disponibles")]
        public async Task<IActionResult> GetHorariosDisponibles()
        {
            try
            {
                var horarios = await _horarioService.GetHorariosDisponiblesAsync();
                return Ok(horarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> GetHorariosByUsuario(int usuarioId)
        {
            try
            {
                var horarios = await _horarioService.GetHorariosByUsuarioAsync(usuarioId);
                return Ok(horarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHorario(int id, [FromBody] HorarioCreateDto horarioDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var horario = await _horarioService.UpdateHorarioAsync(id, horarioDto);
                return Ok(horario);
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
        public async Task<IActionResult> DeleteHorario(int id)
        {
            try
            {
                var result = await _horarioService.DeleteHorarioAsync(id);
                if (result)
                {
                    return Ok(new { message = "Horario eliminado exitosamente" });
                }
                return NotFound(new { message = "Horario no encontrado" });
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
