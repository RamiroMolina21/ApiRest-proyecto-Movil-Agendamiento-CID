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

public class NotificacionRepository : INotificacionRepository
{
    private readonly AgendamientoDbContext _context;

    public NotificacionRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<Notificacion> CreateAsync(Notificacion notificacion)
    {
        _context.Notificaciones.Add(notificacion);
        await _context.SaveChangesAsync();
        return notificacion;
    }

    public async Task<Notificacion> GetByIdAsync(int id)
    {
        return await _context.Notificaciones
            .Include(n => n.Usuario)
            .FirstOrDefaultAsync(n => n.idNotificacion == id);
    }

    public async Task<List<Notificacion>> GetAllAsync()
    {
        return await _context.Notificaciones
            .Include(n => n.Usuario)
            .ToListAsync();
    }

    public async Task<Notificacion> UpdateAsync(Notificacion notificacion)
    {
        _context.Notificaciones.Update(notificacion);
        await _context.SaveChangesAsync();
        return notificacion;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var notificacion = await _context.Notificaciones.FindAsync(id);
        if (notificacion == null)
            return false;

        _context.Notificaciones.Remove(notificacion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Notificacion>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Notificaciones
            .Include(n => n.Usuario)
            .Where(n => n.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

    public async Task<List<Notificacion>> GetByFechaAsync(DateTime fechaDesde)
    {
        return await _context.Notificaciones
            .Include(n => n.Usuario)
            .Where(n => n.fecha >= fechaDesde)
            .ToListAsync();
    }
}
