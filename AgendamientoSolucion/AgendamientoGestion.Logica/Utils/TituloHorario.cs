using System.Globalization;

namespace AgendamientoGestion.Logica.Utils;

/// <summary>
/// Título de Horario cuando el cliente no envía uno (la columna Horario.titulo es VARCHAR(100) NOT NULL).
/// Formato: "{espacio} · {dd/MM/yyyy} {HH:mm}–{HH:mm}"; si el horario abarca varios días:
/// "{espacio} · {dd/MM/yyyy}–{dd/MM/yyyy} {HH:mm}–{HH:mm}".
/// Es determinista (mismos datos → mismo título), lo que permite reconocer en un PUT si el título
/// guardado fue generado y regenerarlo. Los caracteres "·" y "–" existen en la página de códigos 1252
/// de la collation de la BD (Modern_Spanish_CI_AS), así que se guardan sin pérdida en VARCHAR.
/// </summary>
public static class TituloHorario
{
    public const int LongitudMaxima = 100;

    public static string Generar(string? espacio, DateTime fechaInicio, DateTime fechaFin, DateTime horaInicio, DateTime horaFin)
    {
        var inv = CultureInfo.InvariantCulture;
        var dias = fechaInicio.Date == fechaFin.Date
            ? fechaInicio.ToString("dd/MM/yyyy", inv)
            : $"{fechaInicio.ToString("dd/MM/yyyy", inv)}–{fechaFin.ToString("dd/MM/yyyy", inv)}";
        var sufijo = $" · {dias} {horaInicio.ToString("HH:mm", inv)}–{horaFin.ToString("HH:mm", inv)}";

        var lugar = string.IsNullOrWhiteSpace(espacio) ? "Horario" : espacio.Trim();
        var maxLugar = LongitudMaxima - sufijo.Length;
        if (lugar.Length > maxLugar) lugar = lugar[..maxLugar].TrimEnd();

        return lugar + sufijo;
    }
}
