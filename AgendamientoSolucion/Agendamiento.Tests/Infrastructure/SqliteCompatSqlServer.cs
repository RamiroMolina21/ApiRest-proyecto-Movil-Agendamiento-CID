using AgendamientoGestion.Persistencia;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Agendamiento.Tests.Infrastructure;

/// <summary>
/// Emula en SQLite lo que la búsqueda de usuarios usa de SQL Server: la collation insensible a mayúsculas y
/// tildes (<see cref="UsuarioRepository.CollationBusqueda"/>) y LIKE ... ESCAPE con esa semántica. SQLite no
/// aplica collations a LIKE y su LIKE solo ignora mayúsculas ASCII, así que se sustituye la función like().
/// </summary>
public static class SqliteCompatSqlServer
{
    public static void Registrar(SqliteConnection connection)
    {
        connection.CreateCollation(UsuarioRepository.CollationBusqueda,
            (a, b) => string.Compare(Normalizar(a), Normalizar(b), StringComparison.Ordinal));

        // like(patrón, valor) y like(patrón, valor, escape): firma de SQLite para "valor LIKE patrón [ESCAPE e]".
        connection.CreateFunction("like", (string? patron, string? valor) => Like(patron, valor, null));
        connection.CreateFunction("like", (string? patron, string? valor, string? escape) => Like(patron, valor, escape));
    }

    private static bool Like(string? patron, string? valor, string? escape)
    {
        if (patron == null || valor == null) return false;
        var esc = string.IsNullOrEmpty(escape) ? (char?)null : escape[0];
        var regex = new StringBuilder("^");
        var p = Normalizar(patron);
        for (var i = 0; i < p.Length; i++)
        {
            var c = p[i];
            if (esc.HasValue && c == esc.Value && i + 1 < p.Length)
            {
                regex.Append(Regex.Escape(p[++i].ToString()));
            }
            else if (c == '%') regex.Append(".*");
            else if (c == '_') regex.Append('.');
            else regex.Append(Regex.Escape(c.ToString()));
        }
        regex.Append('$');
        return Regex.IsMatch(Normalizar(valor), regex.ToString(), RegexOptions.Singleline);
    }

    /// <summary>Minúsculas y sin diacríticos ("Pérez" → "perez", "Ñ" → "n").</summary>
    private static string Normalizar(string s)
    {
        var descompuesto = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var ch in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
