namespace AgendamientoGestion.Entidades.Constants
{
    /// <summary>
    /// Estados de Horario.estado. Conviven "Disponible" (seed y web) y "Libre" (usado por la lógica de
    /// creación de tutorías). Ambos se interpretan como "disponible" hasta que se decida un valor oficial.
    /// </summary>
    public static class EstadosHorario
    {
        public const string Disponible = "Disponible";
        public const string Libre = "Libre";
        public const string Ocupado = "Ocupado";
        public const string Cancelado = "Cancelado";

        /// <summary>Sinónimos de "disponible" tal como pueden estar guardados.</summary>
        public static readonly string[] Disponibles = { Disponible, Libre };

        public static bool EsDisponible(string? estado) =>
            Disponibles.Any(d => string.Equals(d, estado?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
