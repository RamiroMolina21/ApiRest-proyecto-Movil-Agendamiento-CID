using Agendamiento.Security;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Controllers
{
    /// <summary>
    /// Casos del dashboard de la web marcados como atendidos. Compartidos por todos los Administradores y
    /// Auxiliares (en cualquier equipo). La web calcula los casos; aquí solo se guarda quién atendió qué y cuándo.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = Politicas.GestionWeb)]
    public class AtencionController : ControllerBase
    {
        private readonly IAtencionService _atencionService;

        public AtencionController(IAtencionService atencionService)
        {
            _atencionService = atencionService;
        }

        /// <summary>GET /api/Atencion/atendidos?tipo= (tipo opcional).</summary>
        [HttpGet("atendidos")]
        [ProducesResponseType(typeof(List<AsuntoAtendidoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<AsuntoAtendidoDto>>> GetAtendidos([FromQuery] string? tipo)
        {
            return Ok(await _atencionService.GetAtendidosAsync(tipo));
        }

        /// <summary>POST /api/Atencion/atendidos {tipo, claves}. Idempotente; el usuario sale del token.</summary>
        [HttpPost("atendidos")]
        [ProducesResponseType(typeof(List<AsuntoAtendidoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<AsuntoAtendidoDto>>> MarcarAtendidos([FromBody] MarcarAtendidosDto dto)
        {
            var usuarioId = User.GetUsuarioId() ?? throw new UnauthorizedException("Token sin identificador de usuario");
            return Ok(await _atencionService.MarcarAtendidosAsync(dto, usuarioId));
        }

        /// <summary>DELETE /api/Atencion/atendidos/{tipo}/{clave}: deshacer. Idempotente (204 aunque no existiera).</summary>
        [HttpDelete("atendidos/{tipo}/{clave:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Desmarcar(string tipo, int clave)
        {
            await _atencionService.DesmarcarAsync(tipo, clave);
            return NoContent();
        }
    }
}
