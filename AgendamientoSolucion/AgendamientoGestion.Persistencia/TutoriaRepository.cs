using AgendamientoGestion.Entidades.Constants;
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
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .FirstOrDefaultAsync(t => t.idTutoria == id);
    }

    public async Task<List<Tutoria>> GetAllAsync()
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
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

    public async Task<bool> DeleteWithDependenciesAsync(int id)
    {
        var tutoria = await _context.Tutorias.FindAsync(id);
        if (tutoria == null)
            return false;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Las FK son ON DELETE NO ACTION: hay que borrar los dependientes antes que la tutoría.
        _context.Informes.RemoveRange(
            await _context.Informes.Where(i => i.Tutoria_idTutoria == id).ToListAsync());
        _context.Retroalimentaciones.RemoveRange(
            await _context.Retroalimentaciones.Where(r => r.Tutoria_idTutoria == id).ToListAsync());
        _context.TutoriaEstudiantes.RemoveRange(
            await _context.TutoriaEstudiantes.Where(te => te.Tutoria_idTutoria == id).ToListAsync());
        _context.EncuestasSatisfaccion.RemoveRange(
            await _context.EncuestasSatisfaccion.Where(e => e.Tutoria_idTutoria == id).ToListAsync());
        _context.Tutorias.Remove(tutoria);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<List<Tutoria>> GetByIdsAsync(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
            return new List<Tutoria>();

        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => ids.Contains(t.idTutoria))
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByHorarioAsync(int horarioId)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.Horario_idHorario == horarioId)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByEstadoAsync(string estado)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.estado == estado)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByEstadoAndUsuarioAsync(string estado, int usuarioId)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.estado == estado && t.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByEstadosAndUsuarioAsync(IReadOnlyCollection<string> estadosLower, int usuarioId)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.Usuario_idUsuario == usuarioId && estadosLower.Contains(t.estado.ToLower()))
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByDuenoHorarioAsync(int usuarioId, IReadOnlyCollection<string>? estadosLower = null)
    {
        var query = _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.Horario.Usuario_idUsuario == usuarioId);

        if (estadosLower != null)
            query = query.Where(t => estadosLower.Contains(t.estado.ToLower()));

        return await query.OrderBy(t => t.fechaHora).ToListAsync();
    }

    public async Task BloquearAsync(int tutoriaId)
    {
        // UPDATE sin cambio real: toma un bloqueo exclusivo de la fila hasta el fin de la transacción,
        // de modo que dos peticiones sobre la misma tutoría se serializan (sin deadlocks ni rangos).
        await _context.Tutorias
            .Where(t => t.idTutoria == tutoriaId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.estado, t => t.estado));
    }

    public async Task<bool> MarcarFinalizadaAsync(int tutoriaId)
    {
        var filas = await _context.Tutorias
            .Where(t => t.idTutoria == tutoriaId &&
                        t.estado.ToLower() != "finalizada" &&
                        t.estado.ToLower() != "completada" &&
                        t.estado.ToLower() != "cancelada")
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.estado, EstadosTutoria.ValorAlFinalizar));

        if (filas == 0)
            return false;

        var cargada = _context.ChangeTracker.Entries<Tutoria>().FirstOrDefault(e => e.Entity.idTutoria == tutoriaId);
        if (cargada != null)
        {
            cargada.Entity.estado = EstadosTutoria.ValorAlFinalizar;
            cargada.Property(t => t.estado).OriginalValue = EstadosTutoria.ValorAlFinalizar;
        }

        return true;
    }

    public async Task<List<Tutoria>> GetByFechaAsync(DateTime fecha)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.fechaHora.Date == fecha.Date)
            .ToListAsync();
    }

    public async Task<List<Tutoria>> GetByIdiomaNivelAsync(string idioma, string nivel)
    {
        return await _context.Tutorias
            .Include(t => t.Usuario)
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
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
            .Include(t => t.Horario).ThenInclude(h => h.Usuario)
            .Where(t => t.fechaHora >= tiempoInicio && 
                       t.fechaHora <= tiempoFin &&
                       t.fechaHora > ahora &&
                       // Antes solo "programada"/"activo": las tutorías creadas por web/MAUI ("Pendiente")
                       // nunca recibían recordatorio. Comparación literal (OR) para máxima compatibilidad SQL.
                       (t.estado.ToLower() == "pendiente" ||
                        t.estado.ToLower() == "programada" ||
                        t.estado.ToLower() == "activo"))
            .ToListAsync();
    }
}
