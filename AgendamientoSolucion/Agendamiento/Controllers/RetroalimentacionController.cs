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
    public class RetroalimentacionController : ControllerBase
    {
        private readonly IRetroalimentacionService _retroalimentacionService;

        public RetroalimentacionController(IRetroalimentacionService retroalimentacionService)
        {
            _retroalimentacionService = retroalimentacionService;
        }

        /// <summary>Lo usa MAUI al calificar una tutoría antes de finalizarla.</summary>
        [HttpPost]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> CreateRetroalimentacion([FromBody] RetroalimentacionCreateDto retroalimentacionDto)
        {
            var retroalimentacion = await _retroalimentacionService.CreateRetroalimentacionAsync(retroalimentacionDto, User.ComoSolicitante());
            return CreatedAtAction(nameof(GetRetroalimentacion), new { id = retroalimentacion.IdRetroalimentacion }, retroalimentacion);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetRetroalimentacion(int id)
        {
            return Ok(await _retroalimentacionService.GetRetroalimentacionByIdAsync(id));
        }

        [HttpGet]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetAllRetroalimentaciones()
        {
            return Ok(await _retroalimentacionService.GetAllRetroalimentacionesAsync());
        }

        /// <summary>Admin/Aux: cualquiera. Docente/Tutor: solo tutorías en las que participan (403 si no).</summary>
        [HttpGet("tutoria/{tutoriaId}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetRetroalimentacionesByTutoria(int tutoriaId)
        {
            return Ok(await _retroalimentacionService.GetRetroalimentacionesByTutoriaAsync(tutoriaId, User.ComoSolicitante()));
        }

        [HttpGet("usuario/{usuarioId}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetRetroalimentacionesByUsuario(int usuarioId)
        {
            return Ok(await _retroalimentacionService.GetRetroalimentacionesByUsuarioAsync(usuarioId));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> UpdateRetroalimentacion(int id, [FromBody] RetroalimentacionCreateDto retroalimentacionDto)
        {
            return Ok(await _retroalimentacionService.UpdateRetroalimentacionAsync(id, retroalimentacionDto));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> DeleteRetroalimentacion(int id)
        {
            if (!await _retroalimentacionService.DeleteRetroalimentacionAsync(id))
            {
                throw new NotFoundException("Retroalimentación no encontrada");
            }

            return Ok(new { message = "Retroalimentación eliminada exitosamente" });
        }
    }
}
