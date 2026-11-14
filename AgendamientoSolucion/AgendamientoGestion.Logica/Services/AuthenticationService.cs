using AgendamientoGestion.Entidades.Constants;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendamientoGestion.Logica.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        /// <summary>Mensaje único para correo inexistente o contraseña errónea (evita enumerar usuarios).</summary>
        public const string MensajeCredencialesInvalidas = "Correo o contraseña incorrectos";

        public const string MensajeSinAcceso = "El usuario no tiene acceso a la aplicación";

        public const string MensajeCuentaDesactivada = "La cuenta está desactivada. Contacte con un administrador";
        // Mismo texto en Agendamiento.Security.ValidacionCuentaJwt (401 de peticiones con token de una cuenta desactivada).

        // Hash BCrypt válido de una cadena aleatoria: se verifica cuando el correo no existe para que el tiempo
        // de respuesta sea similar al de un correo existente (mitiga enumeración por tiempo).
        private static readonly string HashFicticio = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRolRepository _rolRepository;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUsuarioRepository usuarioRepository,
            IRolRepository rolRepository,
            ILogger<AuthenticationService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _rolRepository = rolRepository;
            _logger = logger;
        }

        public async Task<UsuarioResponseDto> LoginAsync(LoginDto loginDto)
        {
            EmailValidator.ValidateEmail(loginDto.Correo, "Correo");

            var usuario = await _usuarioRepository.GetByEmailAsync(loginDto.Correo.Trim());
            if (usuario == null)
            {
                await ValidatePasswordAsync(loginDto.Contrasena, HashFicticio);
                throw new UnauthorizedException(MensajeCredencialesInvalidas);
            }

            if (!await ValidatePasswordAsync(loginDto.Contrasena, usuario.contrasenaHash))
            {
                throw new UnauthorizedException(MensajeCredencialesInvalidas);
            }

            // La contraseña es correcta: aquí ya se puede revelar que la cuenta existe pero no tiene acceso.
            var rol = usuario.Rol ?? await _rolRepository.GetByIdAsync(usuario.Rol_idRol);
            var rolCanonico = Roles.NormalizarRolConAcceso(rol?.tipoRol);
            if (rol == null || rolCanonico == null)
            {
                throw new ForbiddenException(MensajeSinAcceso);
            }

            // Solo tras validar la contraseña: no revela a terceros si una cuenta está desactivada.
            if (!usuario.activo)
            {
                throw new ForbiddenException(MensajeCuentaDesactivada);
            }

            return UsuarioService.Map(usuario, rol.tipoRol);
        }

        public const string MensajeContrasenaActualIncorrecta = "La contraseña actual no es correcta";

        public async Task CambiarContrasenaAsync(int usuarioId, CambiarContrasenaDto dto)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            if (usuario == null)
            {
                // El usuario fue borrado después de emitir el token (igual que GET /Auth/me).
                throw new UnauthorizedException("El usuario del token ya no existe");
            }

            // 400 y no 401: el token es válido; un 401 haría que los clientes cierren la sesión.
            if (!await ValidatePasswordAsync(dto.ContrasenaActual, usuario.contrasenaHash))
            {
                throw new ValidationException(MensajeContrasenaActualIncorrecta);
            }

            var nueva = dto.ContrasenaNueva ?? string.Empty;
            if (nueva.Length < 8 || System.Text.Encoding.UTF8.GetByteCount(nueva) > 72)
            {
                throw new ValidationException("La nueva contraseña debe tener entre 8 y 72 caracteres (máximo 72 bytes)");
            }

            if (string.IsNullOrWhiteSpace(nueva))
            {
                throw new ValidationException("La nueva contraseña no puede estar formada solo por espacios");
            }

            if (nueva == dto.ContrasenaActual)
            {
                throw new ValidationException("La nueva contraseña debe ser distinta de la actual");
            }

            if (string.Equals(nueva.Trim(), usuario.correo?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("La nueva contraseña no puede ser igual a su correo");
            }

            usuario.contrasenaHash = BCrypt.Net.BCrypt.HashPassword(nueva);
            await _usuarioRepository.UpdateAsync(usuario);
            _logger.LogInformation("El usuario {UsuarioId} cambió su contraseña", usuarioId);
        }

        /// <summary>
        /// Verifica la contraseña contra un hash BCrypt. Si el hash guardado no es BCrypt (p. ej. los valores
        /// "hash12345" del seed) devuelve false en lugar de lanzar SaltParseException (antes → HTTP 500).
        /// </summary>
        public Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return Task.FromResult(false);

            try
            {
                return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, hashedPassword));
            }
            catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
            {
                _logger.LogWarning("Se encontró un hash de contraseña con formato no BCrypt; el login se rechaza. Restablezca la contraseña del usuario.");
                return Task.FromResult(false);
            }
        }
    }
}
