using AgendamientoGestion.Entidades.Entities;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface IRestablecimientoContrasenaRepository
{
    /// <summary>Invalida (fechaUso = ahora) las solicitudes pendientes del usuario y guarda la nueva.</summary>
    Task CrearInvalidandoAnterioresAsync(RestablecimientoContrasena nuevo, DateTime ahoraUtc);

    /// <summary>Solicitud pendiente (no usada, no caducada) con ese hash, con Usuario y Rol. null si no hay.</summary>
    Task<RestablecimientoContrasena?> GetVigentePorHashAsync(string tokenHash, DateTime ahoraUtc);

    /// <summary>
    /// Marca como usada la solicitud (UPDATE condicional: solo una petición concurrente gana) y todas las pendientes
    /// del usuario. false si ya estaba usada.
    /// </summary>
    Task<bool> ConsumirAsync(int idRestablecimiento, int usuarioId, DateTime ahoraUtc);
}