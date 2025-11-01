using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces
{
    public interface INotificacionRepository
    {
        Task<Notificacion> CreateAsync(Notificacion notificacion);
        Task<Notificacion> GetByIdAsync(int id);
        Task<List<Notificacion>> GetAllAsync();
        Task<Notificacion> UpdateAsync(Notificacion notificacion);
        Task<bool> DeleteAsync(int id);
        Task<List<Notificacion>> GetByUsuarioAsync(int usuarioId);
    }
}
