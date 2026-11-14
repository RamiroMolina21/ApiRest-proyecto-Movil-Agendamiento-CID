using System.Globalization;

namespace AgendamientoGestion.Logica.Correo;

/// <summary>
/// Modelo tipado de un correo. Cada tipo de correo tiene su clase (abajo) y sus plantillas
/// Correo/Plantillas/{Plantilla}.html y .txt. Los valores se escapan siempre al renderizar.
/// Para añadir un correo: nueva clase aquí + sus dos plantillas + un ejemplo en <see cref="EjemplosCorreo"/>.
/// </summary>
public interface IModeloCorreo
{
    /// <summary>Nombre de la plantilla (archivos Correo/Plantillas/{Plantilla}.html y .txt).</summary>
    string Plantilla { get; }
    string Asunto { get; }
    /// <summary>Título del encabezado del layout.</summary>
    string Titulo { get; }
    /// <summary>Color del encabezado del layout (hex). Decisión visual: la puede cambiar el diseño.</summary>
    string ColorEncabezado { get; }
    /// <summary>Texto corto que muchos clientes muestran junto al asunto (preheader). Oculto en el cuerpo.</summary>
    string Preencabezado { get; }
    IReadOnlyDictionary<string, object?> Valores();
}

internal static class Formato
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    public static string FechaHora(DateTime f) => f.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    public static string Fecha(DateTime f) => f.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}

/// <summary>Nueva tutoría asignada (a un estudiante o, si la tutoría no tiene estudiantes, al docente/tutor).</summary>
public sealed record NuevaTutoriaCorreo(
    string DestinatarioNombre, int IdTutoria, string Idioma, string Nivel, string Tema, string Modalidad,
    DateTime FechaHora, string? Espacio) : IModeloCorreo
{
    public string Plantilla => "nueva-tutoria";
    public string Asunto => $"Nueva Tutoría Asignada - {Idioma} {Nivel}";
    public string Titulo => "Tienes una nueva tutoría";
    public string ColorEncabezado => "#7b42f6";
    public string Preencabezado => $"{Idioma} {Nivel} · {Formato.FechaHora(FechaHora)} · {Tema}";

    public IReadOnlyDictionary<string, object?> Valores() => new Dictionary<string, object?>
    {
        ["destinatarioNombre"] = DestinatarioNombre,
        ["idTutoria"] = IdTutoria,
        ["idioma"] = Idioma,
        ["nivel"] = Nivel,
        ["tema"] = Tema,
        ["modalidad"] = Modalidad,
        ["fechaHora"] = Formato.FechaHora(FechaHora),
        ["espacio"] = Espacio
    };
}

/// <summary>Recordatorio automático (RecordatorioTutoriaBackgroundService, 2 h antes) a los estudiantes.</summary>
public sealed record RecordatorioTutoriaCorreo(
    string DestinatarioNombre, DateTime FechaHora, string Idioma, string Nivel, string Tema, string Modalidad,
    string? Espacio) : IModeloCorreo
{
    public string Plantilla => "recordatorio-tutoria";
    public string Asunto => $"Recordatorio de Tutoría - {Formato.FechaHora(FechaHora)}";
    public string Titulo => "Tu tutoría empieza pronto";
    public string ColorEncabezado => "#d97706";
    public string Preencabezado => $"{Idioma} {Nivel} a las {FechaHora:HH:mm}" + (string.IsNullOrWhiteSpace(Espacio) ? "" : $" en {Espacio}");

    public IReadOnlyDictionary<string, object?> Valores() => new Dictionary<string, object?>
    {
        ["destinatarioNombre"] = DestinatarioNombre,
        ["fechaHora"] = Formato.FechaHora(FechaHora),
        ["idioma"] = Idioma,
        ["nivel"] = Nivel,
        ["tema"] = Tema,
        ["modalidad"] = Modalidad,
        ["espacio"] = Espacio
    };
}

/// <summary>Cambio en una tutoría (PUT /Tutoria/{id} o POST /Notificacion/email/cambio/{id}) a los estudiantes.</summary>
public sealed record CambioTutoriaCorreo(
    string DestinatarioNombre, int IdTutoria, DateTime FechaHora, string Idioma, string Nivel, string Tema,
    string Modalidad, string? Espacio, string? Profesor, string? Motivo, IReadOnlyList<string> Cambios) : IModeloCorreo
{
    public string Plantilla => "cambio-tutoria";
    public string Asunto => $"Cambio en Tutoría - {Idioma} {Nivel}";
    public string Titulo => "Hubo cambios en tu tutoría";
    public string ColorEncabezado => "#ea580c";
    public string Preencabezado => $"Revisa la nueva información de tu tutoría de {Idioma} {Nivel} ({Formato.FechaHora(FechaHora)})";

    public IReadOnlyDictionary<string, object?> Valores() => new Dictionary<string, object?>
    {
        ["destinatarioNombre"] = DestinatarioNombre,
        ["idTutoria"] = IdTutoria,
        ["fechaHora"] = Formato.FechaHora(FechaHora),
        ["idioma"] = Idioma,
        ["nivel"] = Nivel,
        ["tema"] = Tema,
        ["modalidad"] = Modalidad,
        ["espacio"] = Espacio,
        ["profesor"] = string.IsNullOrWhiteSpace(Profesor) ? "Sin asignar" : Profesor,
        ["motivo"] = Motivo,
        ["hayCambios"] = Cambios.Count > 0,
        ["cambios"] = Cambios
    };
}

/// <summary>Cancelación de tutoría. HOY NO SE ENVÍA (no hay flujo de cancelación que lo dispare); se conserva la plantilla.</summary>
public sealed record CancelacionTutoriaCorreo(
    string DestinatarioNombre, int IdTutoria, DateTime FechaHora, string Idioma, string Nivel, string Tema,
    string? Motivo) : IModeloCorreo
{
    public string Plantilla => "cancelacion-tutoria";
    public string Asunto => $"Tutoría Cancelada - {Idioma} {Nivel}";
    public string Titulo => "Tu tutoría se canceló";
    public string ColorEncabezado => "#6b7280";
    public string Preencabezado => $"Se canceló tu tutoría del {Formato.FechaHora(FechaHora)}";

    public IReadOnlyDictionary<string, object?> Valores() => new Dictionary<string, object?>
    {
        ["destinatarioNombre"] = DestinatarioNombre,
        ["idTutoria"] = IdTutoria,
        ["fechaHora"] = Formato.FechaHora(FechaHora),
        ["idioma"] = Idioma,
        ["nivel"] = Nivel,
        ["tema"] = Tema,
        ["motivo"] = Motivo
    };
}

/// <summary>Encuesta de satisfacción tras finalizar la tutoría (enlace con token de un solo uso).</summary>
public sealed record EncuestaSatisfaccionCorreo(
    string DestinatarioNombre, string Idioma, string Nivel, string Tema, string Enlace) : IModeloCorreo
{
    public string Plantilla => "encuesta-satisfaccion";
    public string Asunto => "Encuesta de satisfacción - Tutoría";
    public string Titulo => "¿Cómo te fue en tu tutoría?";
    public string ColorEncabezado => "#0f766e";
    public string Preencabezado => $"Tu opinión sobre la tutoría de {Idioma} {Nivel} · responder toma menos de un minuto";

    public IReadOnlyDictionary<string, object?> Valores() => new Dictionary<string, object?>
    {
        ["destinatarioNombre"] = DestinatarioNombre,
        ["idioma"] = Idioma,
        ["nivel"] = Nivel,
        ["tema"] = Tema,
        ["enlace"] = Enlace
    };
}
