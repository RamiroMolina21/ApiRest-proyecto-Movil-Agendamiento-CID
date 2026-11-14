using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendamientoGestion.Persistencia;

public class AsuntoAtendidoRepository : IAsuntoAtendidoRepository
{
    private readonly AgendamientoDbContext _context;

    public AsuntoAtendidoRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<List<AsuntoAtendido>> GetAsync(string? tipo)
    {
        var query = _context.AsuntosAtendidos.AsNoTracking().Include(a => a.Usuario).AsQueryable();
        if (tipo != null)
            query = query.Where(a => a.tipo == tipo);

        return await query.OrderBy(a => a.tipo).ThenBy(a => a.clave).ToListAsync();
    }

    public async Task<List<AsuntoAtendido>> GetAsync(string tipo, IReadOnlyCollection<int> claves)
    {
        return await _context.AsuntosAtendidos.AsNoTracking()
            .Include(a => a.Usuario)
            .Where(a => a.tipo == tipo && claves.Contains(a.clave))
            .OrderBy(a => a.clave)
            .ToListAsync();
    }

    public async Task<int> AgregarFaltantesAsync(IReadOnlyCollection<AsuntoAtendido> nuevos)
    {
        var insertados = 0;
        var existentes = new HashSet<(string, int)>();
        foreach (var grupo in nuevos.GroupBy(n => n.tipo))
        {
            var claves = grupo.Select(n => n.clave).ToList();
            var ya = await _context.AsuntosAtendidos.AsNoTracking()
                .Where(a => a.tipo == grupo.Key && claves.Contains(a.clave))
                .Select(a => a.clave)
                .ToListAsync();
            foreach (var c in ya) existentes.Add((grupo.Key, c));
        }

        // Los faltantes, uno a uno: el lote es pequeño (≤ límite del servicio) y así una colisión concurrente con
        // otro equipo (índice único tipo+clave) solo descarta esa fila, no todo el lote.
        foreach (var nuevo in nuevos)
        {
            if (!existentes.Add((nuevo.tipo, nuevo.clave))) continue;

            _context.AsuntosAtendidos.Add(nuevo);
            try
            {
                await _context.SaveChangesAsync();
                insertados++;
            }
            catch (DbUpdateException)
            {
                // Otra petición lo insertó entre el AnyAsync y el INSERT. Si de verdad ya existe, es idempotente;
                // si no existe, el error es otro (FK, etc.) y se propaga.
                _context.Entry(nuevo).State = EntityState.Detached;
                if (!await _context.AsuntosAtendidos.AnyAsync(a => a.tipo == nuevo.tipo && a.clave == nuevo.clave))
                    throw;
            }
        }

        return insertados;
    }

    public async Task<bool> DeleteAsync(string tipo, int clave)
    {
        var filas = await _context.AsuntosAtendidos
            .Where(a => a.tipo == tipo && a.clave == clave)
            .ExecuteDeleteAsync();
        return filas > 0;
    }
}
