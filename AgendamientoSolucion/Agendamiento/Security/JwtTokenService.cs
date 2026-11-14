using AgendamientoGestion.Entidades.Constants;
using AgendamientoGestion.Logica.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Agendamiento.Security
{
    public sealed record TokenEmitido(string Token, DateTime ExpiresAtUtc);

    public interface ITokenService
    {
        TokenEmitido CrearToken(UsuarioResponseDto usuario);
    }

    /// <summary>Emite JWT firmados con HMAC-SHA256 (clave simétrica de configuración Jwt:Key).</summary>
    public sealed class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly JsonWebTokenHandler _handler = new();

        public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
        {
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public TokenEmitido CrearToken(UsuarioResponseDto usuario)
        {
            var rol = Roles.NormalizarRolConAcceso(usuario.TipoRol)
                      ?? throw new InvalidOperationException("No se emite token para un rol sin acceso.");

            var ahora = _timeProvider.GetUtcNow().UtcDateTime;
            var expira = ahora.AddMinutes(_options.ExpirationMinutes);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                IssuedAt = ahora,
                NotBefore = ahora,
                Expires = expira,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(_options.KeyBytes), SecurityAlgorithms.HmacSha256),
                Claims = new Dictionary<string, object>
                {
                    [JwtClaims.Sub] = usuario.IdUsuario.ToString(),
                    [JwtClaims.Correo] = usuario.Correo,
                    [JwtClaims.Rol] = rol,
                    [JwtClaims.Nombre] = $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                    [JwtClaims.Jti] = Guid.NewGuid().ToString("N")
                }
            };

            return new TokenEmitido(_handler.CreateToken(descriptor), expira);
        }
    }
}
