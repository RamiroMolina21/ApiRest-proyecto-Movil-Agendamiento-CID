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

public class InformeRepository : IInformeRepository {
    private readonly AgendamientoDbContext _context;

    public InformeRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<Informe> CreateAsync(Informe informe)
    {
        _context.Informes.Add(informe);
        await _context.SaveChangesAsync();
        return informe;
    }

    public async Task<Informe> GetByIdAsync(int id)
    {
        return await _context.Informes
            .Include(i => i.Tutoria)
            .Include(i => i.Usuario)
            .FirstOrDefaultAsync(i => i.idInforme == id);
    }

    public async Task<List<Informe>> GetAllAsync()
    {
        return await _context.Informes
            .Include(i => i.Tutoria)
            .Include(i => i.Usuario)
            .ToListAsync();
    }

    public async Task<List<Informe>> GetByTutoriaAsync(int tutoriaId)
    {
        return await _context.Informes
            .Include(i => i.Tutoria)
            .Include(i => i.Usuario)
            .Where(i => i.Tutoria_idTutoria == tutoriaId)
            .ToListAsync();
    }

    public async Task<List<Informe>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Informes
            .Include(i => i.Tutoria)
            .Include(i => i.Usuario)
            .Where(i => i.Usuario_idUsuario == usuarioId)
            .ToListAsync();
    }

    public async Task<Informe> UpdateAsync(Informe informe)
    {
        _context.Informes.Update(informe);
        await _context.SaveChangesAsync();
        return informe;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var informe = await _context.Informes.FindAsync(id);
        if (informe == null)
            return false;

        _context.Informes.Remove(informe);
        await _context.SaveChangesAsync();
        return true;
    }
}
