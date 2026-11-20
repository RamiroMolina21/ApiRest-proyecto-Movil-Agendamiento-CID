namespace AgendamientoGestion.Entidades.Entities
{
    /// <summary>
    /// Solicitud de restablecimiento de contraseña ("¿Olvidaste tu contraseña?"). Solo se guarda el HASH SHA-256 del
    /// token (el token en claro solo viaja en el enlace del correo). Un solo uso; fechas en UTC.
    /// </summary>
    public class RestablecimientoContrasena
    {
        public int idRestablecimiento { get; set; }
        public int Usuario_idUsuario { get; set; }
        /// <summary>SHA-256 del token en hexadecimal (64 caracteres).</summary>
        public string tokenHash { get; set; } = string.Empty;
        public DateTime fechaCreacion { get; set; }
        public DateTime fechaExpiracion { get; set; }
        /// <summary>Cuándo se usó o se invalidó (por una solicitud nueva o un restablecimiento). null = pendiente.</summary>
        public DateTime? fechaUso { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}