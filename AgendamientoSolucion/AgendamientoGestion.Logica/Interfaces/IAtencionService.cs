using AgendamientoGestion.Logica.Dtos;

namespace AgendamientoGestion.Logica.Interfaces;

/// <summary>Casos del dashboard de la web marcados como atendidos (compartidos entre Administradores y Auxiliares).</summary>
public interface IAtencionService
{
    /// <summary>Todos los atendidos, o los de un tipo. 400 si el tipo no es de la lista cerrada.</summary>
    Task<List<AsuntoAtendidoDto>> GetAtendidosAsync(string? tipo);

    /// <summary>
    /// Marca como atendidas las claves de un tipo, a nombre de <paramref name="usuarioId"/>. Idempotente: las que
    /// ya estaban atendidas conservan quién y cuándo. Devuelve el estado final de todas las claves pedidas.
    /// </summary>
    Task<List<AsuntoAtendidoDto>> MarcarAtendidosAsync(MarcarAtendidosDto dto, int usuarioId);

    /// <summary>Deshace un atendido. Idempotente (no falla si no existía). 400 si el tipo no es válido.</summary>
    Task DesmarcarAsync(string tipo, int clave);
}
