using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendamientoGestion.Persistencia;

public class MetricasRepository : IMetricasRepository
{
    private readonly AgendamientoDbContext _context;

    public MetricasRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<List<(string Estado, int Cantidad)>> ContarTutoriasPorEstadoAsync() =>
        (await _context.Tutorias.AsNoTracking()
            .GroupBy(t => t.estado)
            .Select(g => new { g.Key, Cantidad = g.Count() })
            .ToListAsync())
        .Select(x => (x.Key, x.Cantidad)).ToList();

    public async Task<List<(string? TipoRol, int Cantidad)>> ContarUsuariosPorRolAsync() =>
        (await _context.Usuarios.AsNoTracking()
            .GroupBy(u => u.Rol.tipoRol)
            .Select(g => new { g.Key, Cantidad = g.Count() })
            .ToListAsync())
        .Select(x => ((string?)x.Key, x.Cantidad)).ToList();

    public async Task<List<(string Estado, int Cantidad)>> ContarHorariosPorEstadoAsync() =>
        (await _context.Horarios.AsNoTracking()
            .GroupBy(h => h.estado)
            .Select(g => new { g.Key, Cantidad = g.Count() })
            .ToListAsync())
        .Select(x => (x.Key, x.Cantidad)).ToList();

    public async Task<(decimal Suma, int Cantidad)> SumarCalificacionesAsync()
    {
        // Solo la columna (no la entidad): SUM de decimal no se traduce en SQLite (tests) y así el resultado es
        // exactamente el mismo decimal que calculaba antes Enumerable.Average.
        var valores = await _context.Retroalimentaciones.AsNoTracking().Select(x => x.calificacion).ToListAsync();
        return (valores.Sum(), valores.Count);
    }
}