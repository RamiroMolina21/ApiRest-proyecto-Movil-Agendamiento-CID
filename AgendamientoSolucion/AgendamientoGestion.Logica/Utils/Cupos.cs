using AgendamientoGestion.Logica.Exceptions;

namespace AgendamientoGestion.Logica.Utils;

/// <summary>
/// Horario.cupos es texto en BD/DTO. Significado (decisión de producto): número máximo de estudiantes
/// en la tutoría de ese horario. Se mantiene string para no romper el contrato, pero se valida el contenido.
/// </summary>
public static class Cupos
{
    public const int Maximo = 1000;

    /// <summary>Devuelve el cupo si es un entero 1..Maximo; null para datos antiguos no numéricos (= sin límite).</summary>
    public static int? Interpretar(string? valor) =>
        int.TryParse(valor?.Trim(), out var n) && n >= 1 && n <= Maximo ? n : null;

    /// <summary>Valida el valor recibido en POST/PUT Horario y lo devuelve normalizado ("08" → "8").</summary>
    public static string ValidarYNormalizar(string? valor) =>
        Interpretar(valor)?.ToString()
        ?? throw new ValidationException(MensajeRango);

    public static readonly string MensajeRango = $"Los cupos deben ser un número entero entre 1 y {Maximo}";
    public const string MensajeObligatorio = "Los cupos son obligatorios";

    /// <summary>
    /// Horario.cupos es OPCIONAL desde 2026-11-21 (la capacidad vive en Tutoria.cupos): null/vacío → "";
    /// si viene, se valida 1..Maximo y se normaliza.
    /// </summary>
    public static string ValidarOpcionalHorario(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : ValidarYNormalizar(valor);

    /// <summary>Valida Tutoria.cupos (entero 1..Maximo). null → 400 si es obligatorio.</summary>
    public static int? ValidarTutoria(int? valor, bool obligatorio)
    {
        if (valor == null)
        {
            return obligatorio ? throw new ValidationException(MensajeObligatorio) : null;
        }

        return valor is >= 1 and <= Maximo ? valor : throw new ValidationException(MensajeRango);
    }

    /// <summary>
    /// Capacidad efectiva: Tutoria.cupos; si es null (tutorías anteriores al cambio) los del horario;
    /// si tampoco hay número, null = sin límite.
    /// </summary>
    public static int? Efectivos(int? cuposTutoria, string? cuposHorario) =>
        cuposTutoria ?? Interpretar(cuposHorario);
}
