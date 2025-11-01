using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface IRolRepository {
    Task<Rol> GetByIdAsync(int id);
    Task<List<Rol>> GetAllAsync();
    Task<Rol> GetByTipoAsync(string tipoRol);
}
