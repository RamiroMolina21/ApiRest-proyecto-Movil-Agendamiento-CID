using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces
{
    public interface IHorarioRepository
    {
        Task<Horario> CreateAsync(Horario horario);
        Task<Horario> GetByIdAsync(int id);
        Task<List<Horario>> GetAllAsync();
        Task<Horario> UpdateAsync(Horario horario);
        Task<bool> DeleteAsync(int id);
        Task<List<Horario>> GetByUsuarioAsync(int usuarioId);
        Task<List<Horario>> GetDisponiblesAsync();
    }
}
