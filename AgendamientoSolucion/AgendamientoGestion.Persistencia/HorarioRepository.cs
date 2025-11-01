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

public class HorarioRepository : IHorarioRepository
{
    private readonly AgendamientoDbContext _context;

    public HorarioRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<Horario> CreateAsync(Horario horario)
    {
        _context.Horarios.Add(horario);
        await _context.SaveChangesAsync();
        return horario;
    }

    public async Task<Horario> GetByIdAsync(int id)
    {
        return await _context.Horarios
            .Include(h => h.Usuario)
            .FirstOrDefaultAsync(h => h.idHorario == id);
    }

    public async Task<List<Horario>> GetAllAsync()
    {
        return await _context.Horarios
            .Include(h => h.Usuario)
            .ToListAsync();
    }

    public async Task<Horario> UpdateAsync(Horario horario)
    {
        _context.Horarios.Update(horario);
        await _context.SaveChangesAsync();
        return horario;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var horario = await _context.Horarios.FindAsync(id);
        if (horario == null)
            return false;

        _context.Horarios.Remove(horario);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Horario>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Horarios
            .Include(h => h.Usuario)
            .Where(h => h.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

    public async Task<List<Horario>> GetDisponiblesAsync()
    {
        return await _context.Horarios
            .Include(h => h.Usuario)
            .Where(h => h.estado == "Disponible")
            .ToListAsync();
    }
}
