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

    public async Task<EncuestaSatisfaccion> UpdateAsync(EncuestaSatisfaccion encuesta)
    {
        _context.EncuestasSatisfaccion.Update(encuesta);
        await _context.SaveChangesAsync();
        return encuesta;
    }
}
