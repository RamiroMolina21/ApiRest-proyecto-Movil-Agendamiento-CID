namespace AgendamientoGestion.Persistencia.Interfaces;

/// <summary>Agregados para GET /api/Metricas/generales calculados en SQL (antes se cargaban las tablas enteras).</summary>
public interface IMetricasRepository
{
    /// <summary>Número de tutorías por valor exacto de estado.</summary>
    Task<List<(string Estado, int Cantidad)>> ContarTutoriasPorEstadoAsync();
    /// <summary>Número de usuarios por tipoRol.</summary>
    Task<List<(string? TipoRol, int Cantidad)>> ContarUsuariosPorRolAsync();
    /// <summary>Número de horarios por valor exacto de estado.</summary>
    Task<List<(string Estado, int Cantidad)>> ContarHorariosPorEstadoAsync();
    /// <summary>Suma y número de calificaciones de retroalimentación (el promedio se calcula en C#: mismo resultado decimal).</summary>
    Task<(decimal Suma, int Cantidad)> SumarCalificacionesAsync();
}