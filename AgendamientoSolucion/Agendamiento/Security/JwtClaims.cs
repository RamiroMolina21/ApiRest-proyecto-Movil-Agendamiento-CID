namespace Agendamiento.Security
{
    /// <summary>Nombres de los claims que emite y lee la API (sin el mapeo legado de Microsoft).</summary>
    public static class JwtClaims
    {
        /// <summary>Id del usuario (Usuario.idUsuario) como string.</summary>
        public const string Sub = "sub";
        public const string Correo = "correo";
        /// <summary>Rol canónico: Administrador | Auxiliar | Docente | Tutor.</summary>
        public const string Rol = "rol";
        public const string Nombre = "nombre";
        public const string Jti = "jti";
    }
}
