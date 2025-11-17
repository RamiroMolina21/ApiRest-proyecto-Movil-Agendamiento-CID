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

public class TutoriaEstudianteRepository : ITutoriaEstudianteRepository
{
    private readonly AgendamientoDbContext _context;

    public TutoriaEstudianteRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<TutoriaEstudiante> CreateAsync(TutoriaEstudiante tutoriaEstudiante)
    {
        _context.TutoriaEstudiantes.Add(tutoriaEstudiante);
        await _context.SaveChangesAsync();
        return tutoriaEstudiante;
    }

    public async Task<TutoriaEstudiante> GetByIdAsync(int id)
    {
        return await _context.TutoriaEstudiantes
            .Include(te => te.Tutoria)
            .Include(te => te.Usuario)
            .FirstOrDefaultAsync(te => te.idTutoriaEstudiante == id);
    }

    public async Task<List<TutoriaEstudiante>> GetAllAsync()
    {
        return await _context.TutoriaEstudiantes
            .Include(te => te.Tutoria)
            .Include(te => te.Usuario)
            .ToListAsync();
    }

    public async Task<List<TutoriaEstudiante>> GetByTutoriaAsync(int tutoriaId)
    {
        return await _context.TutoriaEstudiantes
            .Include(te => te.Tutoria)
            .Include(te => te.Usuario)
                .ThenInclude(u => u.Rol)
            .Where(te => te.Tutoria_idTutoria == tutoriaId)
            .ToListAsync();
    }

    public async Task<List<TutoriaEstudiante>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.TutoriaEstudiantes
            .Include(te => te.Tutoria)
            .Include(te => te.Usuario)
            .Where(te => te.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int tutoriaId, int usuarioId)
    {
        return await _context.TutoriaEstudiantes
            .AnyAsync(te => te.Tutoria_idTutoria == tutoriaId && te.Usuario_idUsuario == usuarioId);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tutoriaEstudiante = await _context.TutoriaEstudiantes.FindAsync(id);
        if (tutoriaEstudiante == null)
            return false;

        _context.TutoriaEstudiantes.Remove(tutoriaEstudiante);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByTutoriaAndEstudianteAsync(int tutoriaId, int estudianteId)
    {
        var tutoriaEstudiante = await _context.TutoriaEstudiantes
            .FirstOrDefaultAsync(te => te.Tutoria_idTutoria == tutoriaId && te.Usuario_idUsuario == estudianteId);
        
        if (tutoriaEstudiante == null)
            return false;

        _context.TutoriaEstudiantes.Remove(tutoriaEstudiante);
        await _context.SaveChangesAsync();
        return true;
    }
}

