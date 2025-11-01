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

public class RolRepository : IRolRepository
{
    private readonly AgendamientoDbContext _context;

    public RolRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<Rol> GetByIdAsync(int id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<List<Rol>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    public async Task<Rol> GetByTipoAsync(string tipoRol)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.tipoRol == tipoRol);
    }
}
