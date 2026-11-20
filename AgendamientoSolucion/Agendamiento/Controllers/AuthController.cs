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
        /// <summary>
        /// "¿Olvidaste tu contraseña?". Público. SIEMPRE 200 con el mismo mensaje, exista o no la cuenta; el correo
        /// (si procede) se envía en segundo plano. 400 correo no válido; 429 límite por IP o por correo.
        /// </summary>
        [HttpPost("olvide-contrasena")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitRestablecimiento.PoliticaSolicitud)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public IActionResult OlvideContrasena([FromBody] OlvideContrasenaDto dto,
            [FromServices] AgendamientoGestion.Logica.Services.IRestablecimientoContrasenaService restablecimiento)
        {
            restablecimiento.Solicitar(dto.Correo);
            return Ok(new { message = AgendamientoGestion.Logica.Services.RestablecimientoContrasenaService.MensajeSolicitud });
        }

        /// <summary>Comprueba un enlace de restablecimiento (para que la web muestre el formulario o el error).</summary>
        [HttpGet("restablecer-contrasena/validar")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitRestablecimiento.PoliticaRestablecer)]
        public async Task<IActionResult> ValidarRestablecimiento([FromQuery] string? token,
            [FromServices] AgendamientoGestion.Logica.Services.IRestablecimientoContrasenaService restablecimiento)
        {
            var r = await restablecimiento.ValidarAsync(token);
            return Ok(new { valido = r.Valido, correo = r.Correo, expiraEn = r.ExpiraEn });
        }

        /// <summary>Cambia la contraseña con el token del enlace (un solo uso). 204; 400 token o política; 429.</summary>
        [HttpPost("restablecer-contrasena")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitRestablecimiento.PoliticaRestablecer)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RestablecerContrasena([FromBody] RestablecerContrasenaDto dto,
            [FromServices] AgendamientoGestion.Logica.Services.IRestablecimientoContrasenaService restablecimiento)
        {
            await restablecimiento.RestablecerAsync(dto.Token, dto.ContrasenaNueva);
            return NoContent();
        }

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
