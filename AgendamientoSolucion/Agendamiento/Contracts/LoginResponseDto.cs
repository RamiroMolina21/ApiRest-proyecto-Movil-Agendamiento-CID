using AgendamientoGestion.Logica.Dtos;

namespace Agendamiento.Contracts
{
    /// <summary>Respuesta de POST /api/Auth/login. Se mantiene "usuario" para no romper a los clientes.</summary>
    public sealed class LoginResponseDto
    {
        public string Message { get; init; } = "Login exitoso";

        /// <summary>JWT para enviar en "Authorization: Bearer {token}".</summary>
        public string Token { get; init; } = string.Empty;

        public string TokenType { get; init; } = "Bearer";

        /// <summary>Instante de expiración en UTC (ISO 8601, termina en "Z").</summary>
        public DateTime ExpiresAt { get; init; }

        public UsuarioResponseDto Usuario { get; init; } = default!;
    }
}
