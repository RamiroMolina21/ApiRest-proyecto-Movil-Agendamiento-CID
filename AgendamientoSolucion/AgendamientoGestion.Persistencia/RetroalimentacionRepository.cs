using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia;

public class RetroalimentacionRepository : IRetroalimentacionRepository
{
    private readonly AgendamientoDbContext _context;

    public RetroalimentacionRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<Retroalimentacion> CreateAsync(Retroalimentacion retroalimentacion)
    {
        _context.Retroalimentaciones.Add(retroalimentacion);
        await _context.SaveChangesAsync();
        return retroalimentacion;
    }

    public async Task<Retroalimentacion> GetByIdAsync(int id)
    {
        return await _context.Retroalimentaciones
            .Include(r => r.Usuario)
            .Include(r => r.Tutoria)
            .FirstOrDefaultAsync(r => r.idRetroalimentacion == id);
    }

    public async Task<List<Retroalimentacion>> GetAllAsync()
    {
        return await _context.Retroalimentaciones
            .Include(r => r.Usuario)
            .Include(r => r.Tutoria)
            .ToListAsync();
    }

    public async Task<Retroalimentacion> UpdateAsync(Retroalimentacion retroalimentacion)
    {
        _context.Retroalimentaciones.Update(retroalimentacion);
        await _context.SaveChangesAsync();
        return retroalimentacion;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var retroalimentacion = await _context.Retroalimentaciones.FindAsync(id);
        if (retroalimentacion == null)
            return false;

        _context.Retroalimentaciones.Remove(retroalimentacion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Retroalimentacion>> GetByTutoriaAsync(int tutoriaId)
    {
        return await _context.Retroalimentaciones
            .Include(r => r.Usuario)
            .Include(r => r.Tutoria)
            .Where(r => r.Tutoria_idTutoria == tutoriaId)
            .ToListAsync();
    }

    public async Task<List<Retroalimentacion>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Retroalimentaciones
            .Include(r => r.Usuario)
            .Include(r => r.Tutoria)
            .Where(r => r.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

}
