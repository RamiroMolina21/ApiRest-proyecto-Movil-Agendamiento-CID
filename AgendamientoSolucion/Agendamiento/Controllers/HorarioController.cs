using Agendamiento.Security;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> CreateHorario([FromBody] HorarioCreateDto horarioDto)
        {
            var horario = await _horarioService.CreateHorarioAsync(horarioDto);
            return CreatedAtAction(nameof(GetHorario), new { id = horario.IdHorario }, horario);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetHorario(int id)
        {
            return Ok(await _horarioService.GetHorarioByIdAsync(id));
        }

        [HttpGet]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetAllHorarios()
        {
            return Ok(await _horarioService.GetAllHorariosAsync());
        }

        /// <summary>Horarios con estado "Disponible" o "Libre".</summary>
        [HttpGet("disponibles")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetHorariosDisponibles()
        {
            return Ok(await _horarioService.GetHorariosDisponiblesAsync());
        }

        /// <summary>Docente/Tutor solo pueden pedir sus propios horarios (usuarioId = su id).</summary>
        [HttpGet("usuario/{usuarioId}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetHorariosByUsuario(int usuarioId)
        {
            User.AsegurarAccesoADatosDe(usuarioId);
            return Ok(await _horarioService.GetHorariosByUsuarioAsync(usuarioId));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> UpdateHorario(int id, [FromBody] HorarioCreateDto horarioDto)
        {
            return Ok(await _horarioService.UpdateHorarioAsync(id, horarioDto));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> DeleteHorario(int id)
        {
            if (!await _horarioService.DeleteHorarioAsync(id))
            {
                throw new NotFoundException("Horario no encontrado");
            }

            return Ok(new { message = "Horario eliminado exitosamente" });
        }
    }
}
