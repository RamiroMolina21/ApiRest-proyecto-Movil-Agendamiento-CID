namespace AgendamientoGestion.Entidades.Constants
{
    /// <summary>
    /// Valores de Rol.tipoRol tal como están guardados en la base de datos.
    /// </summary>
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Auxiliar = "Auxiliar";
        public const string Docente = "Docente";
        public const string Tutor = "Tutor";
        public const string Estudiante = "Estudiante";

        /// <summary>Roles que pueden iniciar sesión en alguno de los clientes (web o MAUI).</summary>
        public static readonly IReadOnlyList<string> ConAcceso = new[] { Administrador, Auxiliar, Docente, Tutor };

        /// <summary>Todos los roles conocidos (BD.sql).</summary>
        public static readonly IReadOnlyList<string> Todos = new[] { Administrador, Auxiliar, Docente, Tutor, Estudiante };

        /// <summary>Nombre canónico de cualquier rol conocido ("estudiante" → "Estudiante") o null si no existe.</summary>
        public static string? Normalizar(string? valor) => Todos.FirstOrDefault(r => Es(valor, r));

        public static bool Es(string? valor, string rol) =>
            string.Equals(valor?.Trim(), rol, StringComparison.OrdinalIgnoreCase);

        /// <summary>Devuelve el nombre canónico del rol (p. ej. "docente" → "Docente") o null si no es un rol con acceso.</summary>
        public static string? NormalizarRolConAcceso(string? valor) =>
            ConAcceso.FirstOrDefault(r => Es(valor, r));
    }
}
