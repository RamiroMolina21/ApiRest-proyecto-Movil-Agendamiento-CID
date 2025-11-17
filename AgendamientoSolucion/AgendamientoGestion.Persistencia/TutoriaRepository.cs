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

public class TutoriaRepository : ITutoriaRepository
{
    private readonly AgendamientoDbContext _context;

    public TutoriaRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<Tutoria> CreateAsync(Tutoria tutoria)
    {
        _context.Tutorias.Add(tutoria);
        await _context.SaveChangesAsync();
        return tutoria;
    }

    public async Task<Tutoria> GetByIdAsync(int id)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .FirstOrDefaultAsync(t => t.idTutoria == id);
    }

    public async Task<List<Tutoria>> GetAllAsync()
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .ToListAsync();
    }

    public async Task<Tutoria> UpdateAsync(Tutoria tutoria)
    {
        _context.Tutorias.Update(tutoria);
        await _context.SaveChangesAsync();
        return tutoria;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tutoria = await _context.Tutorias.FindAsync(id);
        if (tutoria == null)
            return false;

        _context.Tutorias.Remove(tutoria);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Tutoria>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .Where(t => t.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByHorarioAsync(int horarioId)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .Where(t => t.Horario_idHorario == horarioId)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByEstadoAsync(string estado)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .Where(t => t.estado == estado)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByFechaAsync(DateTime fecha)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .Where(t => t.fechaHora.Date == fecha.Date)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByIdiomaNivelAsync(string idioma, string nivel)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .Where(t => t.idioma == idioma && t.nivel == nivel)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetProximasParaRecordatorioAsync(TimeSpan tiempoAntes)
    {
        var ahora = DateTime.Now;
        var tiempoObjetivo = ahora.Add(tiempoAntes);
        // Margen de 5 minutos para procesamiento
        var tiempoInicio = tiempoObjetivo.AddMinutes(-5);
        var tiempoFin = tiempoObjetivo.AddMinutes(5);

        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario)
            .Where(t => t.fechaHora >= tiempoInicio && 
                       t.fechaHora <= tiempoFin &&
                       t.fechaHora > ahora &&
                       (t.estado.ToLower() == "programada" || t.estado.ToLower() == "activo"))
            .ToListAsync();
    }
}
