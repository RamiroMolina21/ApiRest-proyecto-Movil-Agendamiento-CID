using System.Collections.Concurrent;
using AgendamientoGestion.Logica.Models;
using Microsoft.Extensions.Options;

namespace AgendamientoGestion.Logica.Correo;

/// <summary>Correo listo para enviar: asunto, versión HTML y versión de texto plano (multipart/alternative).</summary>
public sealed record CorreoRenderizado(string Asunto, string Html, string Texto);

public interface IPlantillasCorreo
{
    CorreoRenderizado Renderizar(IModeloCorreo modelo);
}

/// <summary>
/// Renderiza <c>Correo/Plantillas/{plantilla}.html|.txt</c> (recursos incrustados) dentro de <c>_layout.html|.txt</c>.
/// Todo valor del modelo se escapa (ver <see cref="MotorPlantillas"/>). La lógica de negocio solo construye el modelo;
/// el diseño vive en las plantillas.
/// </summary>
public sealed class PlantillasCorreo : IPlantillasCorreo
{
    public const string NombreSistemaPorDefecto = "Sistema de Agendamiento de Tutorías - CECAR";

    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private readonly SmtpSettings _smtp;

    public PlantillasCorreo(IOptions<SmtpSettings> smtp)
    {
        _smtp = smtp.Value;
    }

    public CorreoRenderizado Renderizar(IModeloCorreo modelo)
    {
        var valores = new Dictionary<string, object?>(modelo.Valores())
        {
            ["contactoCorreo"] = _smtp.DireccionDeContacto
        };

        var html = MotorPlantillas.Renderizar(Leer($"{modelo.Plantilla}.html"), valores, html: true);
        var texto = MotorPlantillas.Renderizar(Leer($"{modelo.Plantilla}.txt"), valores, html: false);

        var layout = new Dictionary<string, object?>
        {
            ["titulo"] = modelo.Titulo,
            ["colorEncabezado"] = modelo.ColorEncabezado,
            ["preencabezado"] = modelo.Preencabezado,
            ["nombreSistema"] = NombreSistemaPorDefecto,
            ["contactoCorreo"] = _smtp.DireccionDeContacto,
            ["anio"] = DateTime.Now.Year,
            ["contenido"] = new HtmlSeguro(html, texto)
        };

        return new CorreoRenderizado(
            modelo.Asunto,
            MotorPlantillas.Renderizar(Leer("_layout.html"), layout, html: true),
            NormalizarTexto(MotorPlantillas.Renderizar(Leer("_layout.txt"), layout, html: false)));
    }

    /// <summary>Lista de plantillas incrustadas (para la previsualización).</summary>
    public static IReadOnlyList<string> Disponibles() =>
        typeof(PlantillasCorreo).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("correo/", StringComparison.Ordinal))
            .Select(n => n["correo/".Length..])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

    private static string Leer(string archivo) => Cache.GetOrAdd(archivo, a =>
    {
        using var stream = typeof(PlantillasCorreo).Assembly.GetManifestResourceStream($"correo/{a}")
            ?? throw new InvalidOperationException($"No existe la plantilla de correo '{a}' (Correo/Plantillas).");
        using var lector = new StreamReader(stream, System.Text.Encoding.UTF8);
        return lector.ReadToEnd();
    });

    /// <summary>Texto plano: saltos de línea CRLF (RFC 5322) y sin más de una línea en blanco seguida.</summary>
    private static string NormalizarTexto(string texto)
    {
        var lineas = texto.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd());
        var resultado = new List<string>();
        foreach (var l in lineas)
        {
            if (l.Length == 0 && resultado.Count > 0 && resultado[^1].Length == 0) continue;
            resultado.Add(l);
        }
        return string.Join("\r\n", resultado).Trim() + "\r\n";
    }
}
