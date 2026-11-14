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
        ?? throw new ValidationException($"Los cupos deben ser un número entero entre 1 y {Maximo}");
}
