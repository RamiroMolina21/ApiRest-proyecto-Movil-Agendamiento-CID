using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Agendamiento.Security
{
    /// <summary>
    /// Tras validar firma y caducidad del JWT, comprueba que la cuenta siga existiendo y activa
    /// (IEstadoCuentaCache, caché de 30 s). Si no, la autenticación falla → 401 en cualquier endpoint protegido,
    /// con un "message" explicativo que ProblemDetailsDefaults toma de HttpContext.Items.
    /// </summary>
    public static class ValidacionCuentaJwt
    {
        public const string ItemMensaje = "auth.mensaje";
        public const string MensajeUsuarioNoExiste = "El usuario del token ya no existe";
        public const string MensajeCredencialesCambiadas = "La contraseña de la cuenta se cambió. Inicia sesión de nuevo";
        public const string MensajeCuentaDesactivada = "La cuenta está desactivada. Contacte con un administrador";

        public static async Task OnTokenValidated(TokenValidatedContext context)
        {
            if (!int.TryParse(context.Principal?.FindFirst(JwtClaims.Sub)?.Value, out var usuarioId))
            {
                context.HttpContext.Items[ItemMensaje] = "Token sin identificador de usuario";
                context.Fail("Token sin identificador de usuario");
                return;
            }

            var cache = context.HttpContext.RequestServices.GetRequiredService<IEstadoCuentaCache>();
            var info = await cache.ObtenerInfoAsync(usuarioId, context.HttpContext.RequestAborted);
            var mensaje = info.Estado switch
            {
                EstadoCuenta.NoExiste => MensajeUsuarioNoExiste,
                EstadoCuenta.Desactivada => MensajeCuentaDesactivada,
                _ => null
            };

            // Contraseña restablecida después de emitir el token: el token (iat anterior) deja de valer.
            if (mensaje == null && info.CredencialesCambiadasEn is DateTime cambio
                && long.TryParse(context.Principal?.FindFirst("iat")?.Value, out var iat)
                && iat < new DateTimeOffset(DateTime.SpecifyKind(cambio, DateTimeKind.Utc)).ToUnixTimeSeconds())
            {
                mensaje = MensajeCredencialesCambiadas;
            }

            if (mensaje != null)
            {
                context.HttpContext.Items[ItemMensaje] = mensaje;
                context.Fail(mensaje);
            }
        }
    }
}
