namespace AgendamientoGestion.Persistencia.Interfaces;

/// <summary>Qué se hizo con cada dependencia al eliminar un usuario (se devuelve en DELETE /api/Usuario/{id}).</summary>
public sealed class ResultadoEliminacionUsuario
{
    /// <summary>Inscripciones del usuario como estudiante (TutoriaEstudiante) borradas.</summary>
    public int InscripcionesEliminadas { get; set; }
    /// <summary>Notificaciones del usuario borradas.</summary>
    public int NotificacionesEliminadas { get; set; }
    /// <summary>Horarios del usuario sin ninguna tutoría, borrados.</summary>
    public int HorariosEliminados { get; set; }
    /// <summary>Horarios del usuario con tutorías: conservados sin dueño (usuarioId null).</summary>
    public int HorariosConservados { get; set; }
    /// <summary>Tutorías asignadas al usuario: conservadas sin asignar (usuarioId null), en cualquier estado.</summary>
    public int TutoriasDesasignadas { get; set; }
    /// <summary>Retroalimentaciones escritas por el usuario: conservadas sin autor.</summary>
    public int RetroalimentacionesConservadas { get; set; }
    /// <summary>Informes del usuario: conservados sin autor.</summary>
    public int InformesConservados { get; set; }
    /// <summary>Asuntos del dashboard que marcó como atendidos: conservados sin autor.</summary>
    public int AtendidosConservados { get; set; }
    /// <summary>Encuestas dirigidas a su correo y sin responder: borradas.</summary>
    public int EncuestasEliminadas { get; set; }
    /// <summary>Encuestas respondidas: conservadas sin nombre ni correo.</summary>
    public int EncuestasAnonimizadas { get; set; }
}
