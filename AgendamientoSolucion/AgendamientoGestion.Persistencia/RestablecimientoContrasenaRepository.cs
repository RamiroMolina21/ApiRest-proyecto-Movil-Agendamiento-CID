using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendamientoGestion.Persistencia;

public class RestablecimientoContrasenaRepository : IRestablecimientoContrasenaRepository
{
    private readonly AgendamientoDbContext _context;

    public RestablecimientoContrasenaRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task CrearInvalidandoAnterioresAsync(RestablecimientoContrasena nuevo, DateTime ahoraUtc)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        await _context.RestablecimientosContrasena
            .Where(r => r.Usuario_idUsuario == nuevo.Usuario_idUsuario && r.fechaUso == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.fechaUso, (DateTime?)ahoraUtc));
        _context.RestablecimientosContrasena.Add(nuevo);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public Task<RestablecimientoContrasena?> GetVigentePorHashAsync(string tokenHash, DateTime ahoraUtc) =>
        _context.RestablecimientosContrasena.AsNoTracking()
            .Include(r => r.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(r => r.tokenHash == tokenHash && r.fechaUso == null && r.fechaExpiracion > ahoraUtc)!;

    public async Task<bool> ConsumirAsync(int idRestablecimiento, int usuarioId, DateTime ahoraUtc)
    {
        var filas = await _context.RestablecimientosContrasena
            .Where(r => r.idRestablecimiento == idRestablecimiento && r.fechaUso == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.fechaUso, (DateTime?)ahoraUtc));
        if (filas == 0)
            return false;

        await _context.RestablecimientosContrasena
            .Where(r => r.Usuario_idUsuario == usuarioId && r.fechaUso == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.fechaUso, (DateTime?)ahoraUtc));
        return true;
    }
}