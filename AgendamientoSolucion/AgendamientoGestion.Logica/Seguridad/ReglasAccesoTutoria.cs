using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Exceptions;

namespace AgendamientoGestion.Logica.Seguridad;

public enum AccionTutoria
{
    /// <summary>Ver la tutoría, sus estudiantes y sus retroalimentaciones.</summary>
    Ver,
    /// <summary>Finalizar (con o sin calificación), registrar retroalimentación y enviar encuestas.</summary>
    Finalizar,
    /// <summary>Agregar o quitar estudiantes.</summary>
    GestionarEstudiantes
}

/// <summary>
/// Regla de propiedad de tutorías (decisión de producto "Docente dueño + Tutor asignado"):
/// <list type="bullet">
/// <item>Administrador/Auxiliar: cualquier tutoría, cualquier acción.</item>
/// <item>Docente: tutorías en las que está asignado (Tutoria.Usuario_idUsuario) o agendadas en un horario
/// suyo (Tutoria.Horario.Usuario_idUsuario). Puede ver, finalizar y gestionar estudiantes.</item>
/// <item>Tutor: solo las asignadas a él. Puede ver y finalizar; NO gestiona estudiantes (paridad MAUI).</item>
/// </list>
/// Se responde 403 (no 404): los ids de tutoría no son secretos y es coherente con AsegurarAccesoADatosDe.
/// </summary>
public static class ReglasAccesoTutoria
{
    public const string MensajeSinAcceso = "No tiene acceso a esta tutoría";
    public const string MensajeTutorSinGestionEstudiantes = "El Tutor no puede agregar ni quitar estudiantes de una tutoría";

    public static bool EsAsignado(Tutoria tutoria, int usuarioId) => tutoria.Usuario_idUsuario == usuarioId;

    /// <summary>Requiere que <see cref="Tutoria.Horario"/> esté cargado (los repositorios lo incluyen).</summary>
    public static bool EsDuenoDelHorario(Tutoria tutoria, int usuarioId) => tutoria.Horario?.Usuario_idUsuario == usuarioId;

    public static bool Puede(Solicitante s, Tutoria tutoria, AccionTutoria accion)
    {
        if (s.EsGestion) return true;

        if (s.EsDocente)
            return EsAsignado(tutoria, s.UsuarioId) || EsDuenoDelHorario(tutoria, s.UsuarioId);

        if (s.EsTutor)
            return accion != AccionTutoria.GestionarEstudiantes && EsAsignado(tutoria, s.UsuarioId);

        return false;
    }

    public static void Asegurar(Solicitante s, Tutoria tutoria, AccionTutoria accion)
    {
        if (Puede(s, tutoria, accion)) return;

        // Mensaje específico para el Tutor que intenta gestionar estudiantes de una tutoría suya.
        if (s.EsTutor && accion == AccionTutoria.GestionarEstudiantes)
            throw new ForbiddenException(MensajeTutorSinGestionEstudiantes);

        throw new ForbiddenException(MensajeSinAcceso);
    }
}
