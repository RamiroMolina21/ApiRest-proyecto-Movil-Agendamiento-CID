using AgendamientoGestion.Entidades.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Agendamiento.Security
{
    /// <summary>
    /// Políticas de autorización. La política por defecto (fallback) exige usuario autenticado en TODO
    /// endpoint que no declare [AllowAnonymous]. Ver docs/api/autenticacion-y-autorizacion.md.
    /// </summary>
    public static class Politicas
    {
        /// <summary>Gestión desde la web: Administrador y Auxiliar.</summary>
        public const string GestionWeb = nameof(GestionWeb);

        /// <summary>Solo Administrador.</summary>
        public const string SoloAdministrador = nameof(SoloAdministrador);

        /// <summary>Cualquier rol con acceso (web o MAUI): Administrador, Auxiliar, Docente, Tutor.</summary>
        public const string Personal = nameof(Personal);

        public static void Registrar(AuthorizationOptions options)
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(GestionWeb, p => p.RequireRole(Roles.Administrador, Roles.Auxiliar));
            options.AddPolicy(SoloAdministrador, p => p.RequireRole(Roles.Administrador));
            options.AddPolicy(Personal, p => p.RequireRole(Roles.ConAcceso));
        }
    }
}
