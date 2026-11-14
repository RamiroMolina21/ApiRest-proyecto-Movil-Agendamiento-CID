using Agendamiento.Contracts;
using Agendamiento.Security;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUsuarioService _usuarioService;
        private readonly ITokenService _tokenService;

        public AuthController(
            IAuthenticationService authenticationService,
            IUsuarioService usuarioService,
            ITokenService tokenService)
        {
            _authenticationService = authenticationService;
            _usuarioService = usuarioService;
            _tokenService = tokenService;
        }

        /// <summary>Valida credenciales y devuelve un JWT. Público y con límite de intentos por IP.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(LoginRateLimitOptions.PolicyName)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            var usuario = await _authenticationService.LoginAsync(loginDto);
            var token = _tokenService.CrearToken(usuario);

            return Ok(new LoginResponseDto
            {
                Message = "Login exitoso",
                Token = token.Token,
                ExpiresAt = DateTime.SpecifyKind(token.ExpiresAtUtc, DateTimeKind.Utc),
                Usuario = usuario
            });
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado (solo la suya: el id sale del token, no de la URL ni del
        /// cuerpo). PUT porque reemplaza un atributo del recurso "usuario actual" y es idempotente en efecto.
        /// Los tokens ya emitidos siguen siendo válidos hasta su expiración.
        /// </summary>
        [HttpPut("contrasena")]
        [Authorize(Policy = Politicas.Personal)]
        [EnableRateLimiting(CambioContrasenaRateLimitOptions.PolicyName)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaDto dto)
        {
            var id = User.GetUsuarioId() ?? throw new UnauthorizedException("Token sin identificador de usuario");
            await _authenticationService.CambiarContrasenaAsync(id, dto);
            return NoContent();
        }

        /// <summary>Devuelve el usuario del token, leído de la BD (útil para restaurar sesión). 401 si fue borrado o desactivado.</summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UsuarioResponseDto>> Me()
        {
            var id = User.GetUsuarioId() ?? throw new UnauthorizedException("Token sin identificador de usuario");
            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                // Defensa adicional a ValidacionCuentaJwt (que usa caché): /me siempre lee la BD.
                if (!usuario.Activo)
                    throw new UnauthorizedException(ValidacionCuentaJwt.MensajeCuentaDesactivada);
                return Ok(usuario);
            }
            catch (NotFoundException)
            {
                // El usuario fue eliminado después de emitir el token.
                throw new UnauthorizedException("El usuario del token ya no existe");
            }
        }
    }
}
