using System.ComponentModel.DataAnnotations;

namespace AgendamientoGestion.Logica.Dtos;

/// <summary>Tipos de asunto del dashboard de la web y qué id usan como clave. Lista cerrada (400 si no está).</summary>
public static class TiposAsunto
{
    public const string TutoriasVencidas = "tutorias-vencidas";                       // clave: idTutoria
    public const string ProximasSinEstudiantes = "proximas-sin-estudiantes";          // clave: idTutoria
    public const string HorariosLibres7d = "horarios-libres-7d";                      // clave: idHorario
    public const string HorariosVencidosDisponibles = "horarios-vencidos-disponibles"; // clave: idHorario
    public const string FinalizadasSinCalificacion = "finalizadas-sin-calificacion";  // clave: idTutoria
    public const string EncuestasSinResponder = "encuestas-sin-responder";            // clave: idTutoria (GET /api/Encuesta/pendientes)

    public static readonly IReadOnlyList<string> Todos = new[]
    {
        TutoriasVencidas, ProximasSinEstudiantes, HorariosLibres7d,
        HorariosVencidosDisponibles, FinalizadasSinCalificacion, EncuestasSinResponder
    };

    /// <summary>Nombre canónico (minúsculas) o null si no es un tipo conocido.</summary>
    public static string? Normalizar(string? tipo) =>
        Todos.FirstOrDefault(t => string.Equals(t, tipo?.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>Cuerpo de POST /api/Atencion/atendidos.</summary>
public class MarcarAtendidosDto
{
    public const int MaximoClaves = 200;

    [Required(ErrorMessage = "El tipo es obligatorio")]
    public string? Tipo { get; set; }

    [Required(ErrorMessage = "Las claves son obligatorias")]
    public List<int>? Claves { get; set; }
}

/// <summary>Un caso atendido: quién y cuándo (UTC).</summary>
public class AsuntoAtendidoDto
{
    public string Tipo { get; set; } = string.Empty;
    public int Clave { get; set; }
    /// <summary>null: quien lo marcó fue eliminado (UsuarioNombre también null).</summary>
    public int? UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public DateTime Fecha { get; set; }
}

/// <summary>Tutoría con encuestas enviadas y aún sin responder (GET /api/Encuesta/pendientes).</summary>
public class EncuestasPendientesDto
{
    public int TutoriaId { get; set; }
    public string Tema { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public int Enviadas { get; set; }
    public int SinResponder { get; set; }
}
