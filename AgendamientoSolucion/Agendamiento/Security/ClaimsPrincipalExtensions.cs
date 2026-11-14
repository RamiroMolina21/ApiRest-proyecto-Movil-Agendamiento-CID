using AgendamientoGestion.Entidades.Constants;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Seguridad;
using System.Security.Claims;

namespace Agendamiento.Security
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUsuarioId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirst(JwtClaims.Sub)?.Value, out var id) ? id : null;

        public static string GetRol(this ClaimsPrincipal user) =>
            user.FindFirst(JwtClaims.Rol)?.Value ?? string.Empty;

        /// <summary>Identidad del solicitante para las reglas de propiedad de la capa de negocio.</summary>
        public static Solicitante ComoSolicitante(this ClaimsPrincipal user) =>
            new(user.GetUsuarioId() ?? throw new UnauthorizedException("Token sin identificador de usuario"), user.GetRol());

        public static bool EsGestionWeb(this ClaimsPrincipal user) =>
            user.IsInRole(Roles.Administrador) || user.IsInRole(Roles.Auxiliar);

        /// <summary>
        /// Anti-BOLA para listados "por usuario": Administrador/Auxiliar pueden consultar a cualquiera;
        /// Docente/Tutor solo sus propios datos (la app MAUI siempre envía el id del usuario autenticado).
        /// </summary>
        public static void AsegurarAccesoADatosDe(this ClaimsPrincipal user, int usuarioId)
        {
            if (user.EsGestionWeb())
                return;

            if (user.GetUsuarioId() != usuarioId)
                throw new ForbiddenException("Solo puede consultar sus propios datos");
        }
    }
}
