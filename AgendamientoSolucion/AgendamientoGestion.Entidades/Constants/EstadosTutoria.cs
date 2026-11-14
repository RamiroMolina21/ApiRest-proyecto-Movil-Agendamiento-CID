namespace AgendamientoGestion.Entidades.Constants
{
    /// <summary>
    /// Estados de Tutoria.estado.
    ///
    /// DECISIÓN PENDIENTE (dueño del producto): hoy conviven sinónimos en los datos.
    ///  - Los clientes (web y MAUI) crean tutorías con "Pendiente"; el seed de BD usa "Programada".
    ///  - PUT /api/Tutoria/{id}/finalizar guarda "Finalizada"; el seed de BD usa "Completada".
    /// Mientras no se decidan los valores oficiales, el backend NO renombra datos guardados:
    /// la lógica que interpreta estados (métricas, recordatorios, validaciones) acepta ambos sinónimos.
    /// Cuando se decida, basta con migrar los datos y reducir estos grupos a un solo valor.
    /// </summary>
    public static class EstadosTutoria
    {
        public const string Pendiente = "Pendiente";
        public const string Programada = "Programada";
        public const string Activo = "Activo";
        public const string Finalizada = "Finalizada";
        public const string Completada = "Completada";
        public const string Cancelada = "Cancelada";

        /// <summary>Valor que guarda el endpoint de finalizar.</summary>
        public const string ValorAlFinalizar = Finalizada;

        /// <summary>Sinónimos de "tutoría por realizarse" (en minúsculas).</summary>
        public static readonly string[] PendientesLower = { "pendiente", "programada", "activo" };

        /// <summary>Sinónimos de "tutoría realizada" (en minúsculas).</summary>
        public static readonly string[] FinalizadosLower = { "finalizada", "completada" };

        public static bool EsPendiente(string? estado) => PendientesLower.Contains(Normalizar(estado));
        public static bool EsFinalizada(string? estado) => FinalizadosLower.Contains(Normalizar(estado));
        public static bool EsCancelada(string? estado) => Normalizar(estado) == "cancelada";

        /// <summary>
        /// Grupo de sinónimos (en minúsculas) para filtrar listados: "pendiente"/"Programada"/"ACTIVO" → los 3
        /// pendientes; "finalizada"/"completada" → ambos; cualquier otro valor → solo ese valor (sin distinguir mayúsculas).
        /// </summary>
        public static string[] SinonimosLower(string? estado)
        {
            var e = Normalizar(estado);
            if (PendientesLower.Contains(e)) return PendientesLower;
            if (FinalizadosLower.Contains(e)) return FinalizadosLower;
            return new[] { e };
        }

        private static string Normalizar(string? estado) => (estado ?? string.Empty).Trim().ToLowerInvariant();
    }
}
