namespace AgendamientoGestion.Entidades.Entities
{
    /// <summary>
    /// Caso del dashboard de la web marcado como atendido (tabla AsuntoAtendido). Compartido entre todos los
    /// Administradores y Auxiliares. Un caso es (tipo, clave): p. ej. ("tutorias-vencidas", idTutoria).
    /// </summary>
    public class AsuntoAtendido
    {
        public int idAsuntoAtendido { get; set; }
        public string tipo { get; set; } = string.Empty;
        public int clave { get; set; }
        /// <summary>Quién lo marcó como atendido.</summary>
        public int? Usuario_idUsuario { get; set; }
        /// <summary>Cuándo (UTC).</summary>
        public DateTime fecha { get; set; }

        public Usuario? Usuario { get; set; }
    }
}
