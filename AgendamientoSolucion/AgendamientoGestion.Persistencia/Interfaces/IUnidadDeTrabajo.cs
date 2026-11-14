using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace AgendamientoGestion.Persistencia.Interfaces;

/// <summary>
/// Transacción que abarca varios repositorios: todos comparten el mismo DbContext (scoped por petición),
/// así que lo que guarden entre Iniciar y Commit es atómico.
/// </summary>
public interface IUnidadDeTrabajo
{
    Task<IDbContextTransaction> IniciarTransaccionAsync(IsolationLevel aislamiento = IsolationLevel.ReadCommitted);
}
