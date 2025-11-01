using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface IRetroalimentacionRepository
{
    Task<Retroalimentacion> CreateAsync(Retroalimentacion retroalimentacion);
    Task<Retroalimentacion> GetByIdAsync(int id);
    Task<List<Retroalimentacion>> GetAllAsync();
    Task<Retroalimentacion> UpdateAsync(Retroalimentacion retroalimentacion);
    Task<bool> DeleteAsync(int id);
    Task<List<Retroalimentacion>> GetByTutoriaAsync(int tutoriaId);
    Task<List<Retroalimentacion>> GetByUsuarioAsync(int usuarioId);
}
