using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendamientoGestion.Persistencia;

public class EncuestaRepository : IEncuestaRepository
{
    private readonly AgendamientoDbContext _context;

    public EncuestaRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<EncuestaSatisfaccion> CreateAsync(EncuestaSatisfaccion encuesta)
    {
        _context.EncuestasSatisfaccion.Add(encuesta);
        await _context.SaveChangesAsync();
        return encuesta;
    }

    public async Task<EncuestaSatisfaccion> GetByIdAsync(int id)
    {
        return await _context.EncuestasSatisfaccion
            .Include(e => e.Tutoria)
            .FirstOrDefaultAsync(e => e.idEncuesta == id);
    }

    public async Task<EncuestaSatisfaccion> GetByTokenAsync(string token)
    {
        return await _context.EncuestasSatisfaccion
            .Include(e => e.Tutoria)
            .FirstOrDefaultAsync(e => e.tokenRespuesta == token);
    }

    public async Task<List<EncuestaSatisfaccion>> GetByTutoriaIdAsync(int tutoriaId)
    {
        return await _context.EncuestasSatisfaccion
            .Include(e => e.Tutoria)
            .Where(e => e.Tutoria_idTutoria == tutoriaId)
            .ToListAsync();
    }

    public async Task<List<EncuestaSatisfaccion>> GetAllAsync()
    {
        return await _context.EncuestasSatisfaccion
            .Include(e => e.Tutoria)
            .ToListAsync();
    }

    public async Task<List<(Tutoria Tutoria, int Enviadas, int SinResponder)>> GetPendientesPorTutoriaAsync()
    {
        // Agregado en SQL (GROUP BY) y luego una sola consulta de las tutorías implicadas.
        var conteos = await _context.EncuestasSatisfaccion.AsNoTracking()
            .GroupBy(e => e.Tutoria_idTutoria)
            .Select(g => new
            {
                TutoriaId = g.Key,
                Enviadas = g.Count(),
                SinResponder = g.Count(e => !e.tokenUsado)
            })
            .Where(x => x.SinResponder > 0)
            .ToListAsync();

        if (conteos.Count == 0)
            return new List<(Tutoria, int, int)>();

        var ids = conteos.Select(c => c.TutoriaId).ToList();
        var tutorias = await _context.Tutorias.AsNoTracking()
            .Where(t => ids.Contains(t.idTutoria))
            .ToDictionaryAsync(t => t.idTutoria);

        return conteos
            .Where(c => tutorias.ContainsKey(c.TutoriaId))
            .Select(c => (tutorias[c.TutoriaId], c.Enviadas, c.SinResponder))
            .OrderByDescending(x => x.Item1.fechaHora).ThenBy(x => x.Item1.idTutoria)
            .ToList();
    }

    public async Task<(int Total, int Respondidas, long SumaCalificacion)> GetTotalesAsync()
    {
        var r = await _context.EncuestasSatisfaccion.AsNoTracking()
            .GroupBy(e => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Respondidas = g.Count(e => e.tokenUsado),
                Suma = g.Sum(e => e.tokenUsado ? (long)e.calificacion : 0L)
            })
            .FirstOrDefaultAsync();
        return r == null ? (0, 0, 0L) : (r.Total, r.Respondidas, r.Suma);
    }

    public async Task<EncuestaSatisfaccion> UpdateAsync(EncuestaSatisfaccion encuesta)
    {
        _context.EncuestasSatisfaccion.Update(encuesta);
        await _context.SaveChangesAsync();
        return encuesta;
    }
}
