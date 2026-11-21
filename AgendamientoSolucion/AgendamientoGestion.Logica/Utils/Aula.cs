namespace AgendamientoGestion.Logica.Utils;

/// <summary>
/// Horario.espacio (aula) es opcional desde 2026-11-20. La columna es VARCHAR(60) NOT NULL: "sin aula" se guarda como "".
/// El texto para mostrarlo lo decide cada cliente; los correos y el Excel usan <see cref="TextoSinAula"/>.
/// </summary>
public static class Aula
{
    public const string TextoSinAula = "Por confirmar";

    public static string Normalizar(string? espacio) => string.IsNullOrWhiteSpace(espacio) ? string.Empty : espacio.Trim();

    public static string ParaMostrar(string? espacio) => string.IsNullOrWhiteSpace(espacio) ? TextoSinAula : espacio.Trim();
}