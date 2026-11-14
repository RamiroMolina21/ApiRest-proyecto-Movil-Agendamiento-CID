using System.Net;
using System.Text.RegularExpressions;

namespace AgendamientoGestion.Logica.Correo;

/// <summary>Conversión simple HTML → texto para correos que no tienen plantilla .txt propia.</summary>
public static class HtmlATexto
{
    private static readonly Regex Estilos = new(@"<(style|script|head)[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Enlaces = new(@"<a\s[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Bloques = new(@"<\s*(br|/p|/div|/h\d|/li|/tr)\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Etiquetas = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex Espacios = new(@"[ \t]+", RegexOptions.Compiled);
    private static readonly Regex Lineas = new(@"(\r?\n\s*){3,}", RegexOptions.Compiled);

    public static string Convertir(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var t = Estilos.Replace(html, string.Empty);
        t = Enlaces.Replace(t, m =>
        {
            var texto = Etiquetas.Replace(m.Groups[2].Value, string.Empty).Trim();
            var url = m.Groups[1].Value;
            return string.IsNullOrEmpty(texto) || texto == url ? url : $"{texto} ({url})";
        });
        t = Bloques.Replace(t, "\n");
        t = Etiquetas.Replace(t, string.Empty);
        t = WebUtility.HtmlDecode(t);
        t = string.Join("\n", t.Split('\n').Select(l => Espacios.Replace(l, " ").Trim()));
        t = Lineas.Replace(t, "\n\n");
        return t.Trim().Replace("\n", "\r\n") + "\r\n";
    }
}
