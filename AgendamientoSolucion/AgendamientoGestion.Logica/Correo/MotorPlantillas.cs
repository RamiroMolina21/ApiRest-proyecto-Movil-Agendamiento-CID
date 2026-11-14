using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text;
using System.Text.RegularExpressions;

namespace AgendamientoGestion.Logica.Correo;

/// <summary>
/// Motor de plantillas mínimo (subconjunto de Mustache), sin dependencias externas:
/// <list type="bullet">
/// <item><c>{{nombre}}</c>: valor ESCAPADO (HTML-encoding en .html; tal cual en .txt). Es la forma normal.</item>
/// <item><c>{{{nombre}}}</c>: valor sin escapar. Solo acepta <see cref="HtmlSeguro"/> (HTML generado por el servidor,
///   p. ej. el contenido ya renderizado dentro del layout); con cualquier otro tipo lanza excepción.</item>
/// <item><c>{{#nombre}}...{{/nombre}}</c>: sección. Se muestra si el valor es "verdadero" (no null, no "", true,
///   lista no vacía). Si es una lista, se repite por elemento: dentro, <c>{{.}}</c> es el elemento, o sus claves
///   si el elemento es un diccionario.</item>
/// <item><c>{{^nombre}}...{{/nombre}}</c>: sección inversa (si el valor es falso/vacío).</item>
/// <item><c>{{! comentario }}</c>: se elimina.</item>
/// </list>
/// Una variable que no existe en el modelo es un ERROR (detecta erratas de la plantilla en los tests de render).
/// </summary>
public static class MotorPlantillas
{
    private static readonly Regex Seccion = new(
        @"\{\{([#^])\s*([\w.]+)\s*\}\}(.*?)\{\{/\s*\2\s*\}\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Triple = new(@"\{\{\{\s*([\w.]+)\s*\}\}\}", RegexOptions.Compiled);
    private static readonly Regex Doble = new(@"\{\{\s*([\w.]+|\.)\s*\}\}", RegexOptions.Compiled);
    /// <summary>Escapa &lt; &gt; &amp; " ' (y controles) pero deja tildes, ñ y emojis legibles (UTF-8).</summary>
    private static readonly HtmlEncoder Codificador = HtmlEncoder.Create(UnicodeRanges.All);

    private const char LlaveAbre = '\uE000';
    private const char LlaveCierra = '\uE001';

    private static string Proteger(string valor) => valor.Replace('{', LlaveAbre).Replace('}', LlaveCierra);

    private static readonly Regex Comentario = new(@"\{\{!.*?\}\}", RegexOptions.Singleline | RegexOptions.Compiled);

    public static string Renderizar(string plantilla, IReadOnlyDictionary<string, object?> valores, bool html)
    {
        var sinComentarios = Comentario.Replace(plantilla, string.Empty);
        var resultado = RenderizarEn(sinComentarios, new[] { valores }, html, item: null);
        // Las llaves de los VALORES se protegieron con centinelas para que un dato como "{{x}}" nunca se
        // interprete como variable (inyección de plantilla). Aquí se restauran.
        return resultado.Replace(LlaveAbre, '{').Replace(LlaveCierra, '}');
    }

    private static string RenderizarEn(string texto, IReadOnlyList<IReadOnlyDictionary<string, object?>> ambitos, bool html, object? item)
    {
        // Secciones primero (pueden anidarse: se resuelven de fuera hacia dentro con la recursión).
        texto = Seccion.Replace(texto, m =>
        {
            var inversa = m.Groups[1].Value == "^";
            var valor = Buscar(m.Groups[2].Value, ambitos, item);
            var cuerpo = m.Groups[3].Value;

            if (inversa)
                return EsVerdadero(valor) ? string.Empty : RenderizarEn(cuerpo, ambitos, html, item);
            if (!EsVerdadero(valor))
                return string.Empty;

            if (valor is IEnumerable lista and not string)
            {
                var sb = new StringBuilder();
                foreach (var elemento in lista)
                {
                    var nuevosAmbitos = elemento is IReadOnlyDictionary<string, object?> dic
                        ? new List<IReadOnlyDictionary<string, object?>>(ambitos) { dic }
                        : ambitos;
                    sb.Append(RenderizarEn(cuerpo, nuevosAmbitos, html, elemento));
                }
                return sb.ToString();
            }

            return RenderizarEn(cuerpo, ambitos, html, item);
        });

        texto = Triple.Replace(texto, m =>
        {
            var valor = Buscar(m.Groups[1].Value, ambitos, item);
            return valor switch
            {
                null => string.Empty,
                HtmlSeguro h => Proteger(html ? h.Html : h.Texto),
                _ => throw new InvalidOperationException(
                    $"La variable '{m.Groups[1].Value}' se usa sin escapar ({{{{{{...}}}}}}) pero no es HtmlSeguro. Use {{{{...}}}}.")
            };
        });

        texto = Doble.Replace(texto, m =>
        {
            var valor = Buscar(m.Groups[1].Value, ambitos, item);
            var cadena = Formatear(valor);
            return Proteger(html ? Codificador.Encode(cadena) : cadena);
        });

        return texto;
    }

    private static object? Buscar(string nombre, IReadOnlyList<IReadOnlyDictionary<string, object?>> ambitos, object? item)
    {
        if (nombre == ".")
            return item;

        for (var i = ambitos.Count - 1; i >= 0; i--)
        {
            if (ambitos[i].TryGetValue(nombre, out var valor))
                return valor;
        }

        throw new KeyNotFoundException($"La plantilla usa '{{{{{nombre}}}}}' pero el modelo no tiene ese valor.");
    }

    private static bool EsVerdadero(object? valor) => valor switch
    {
        null => false,
        bool b => b,
        string s => !string.IsNullOrWhiteSpace(s),
        HtmlSeguro h => !string.IsNullOrWhiteSpace(h.Html),
        IEnumerable e => e.GetEnumerator().MoveNext(),
        _ => true
    };

    private static string Formatear(object? valor) => valor switch
    {
        null => string.Empty,
        HtmlSeguro h => h.Texto,
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.GetCultureInfo("es-CO")),
        _ => valor.ToString() ?? string.Empty
    };
}

/// <summary>Fragmento HTML generado por el servidor (nunca con datos de usuario sin escapar) y su versión de texto.</summary>
public sealed record HtmlSeguro(string Html, string Texto);
