using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace AgendamientoGestion.Persistencia;

public class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly AgendamientoDbContext _context;

    public UnidadDeTrabajo(AgendamientoDbContext context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> IniciarTransaccionAsync(IsolationLevel aislamiento = IsolationLevel.ReadCommitted) =>
        _context.Database.BeginTransactionAsync(aislamiento);
}
