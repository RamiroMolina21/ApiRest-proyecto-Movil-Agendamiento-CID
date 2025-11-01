using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface IInformeRepository {
    Task<Informe> CreateAsync(Informe informe);
    Task<Informe> GetByIdAsync(int id);
    Task<List<Informe>> GetAllAsync();
    Task<List<Informe>> GetByTutoriaAsync(int tutoriaId);
    Task<List<Informe>> GetByUsuarioAsync(int usuarioId);
    Task<Informe> UpdateAsync(Informe informe);
    Task<bool> DeleteAsync(int id);
}
