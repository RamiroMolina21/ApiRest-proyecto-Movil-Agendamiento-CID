using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface ITutoriaRepository
{
    Task<Tutoria> CreateAsync(Tutoria tutoria);
    Task<Tutoria> GetByIdAsync(int id);
    Task<List<Tutoria>> GetAllAsync();
    Task<Tutoria> UpdateAsync(Tutoria tutoria);
    Task<bool> DeleteAsync(int id);
    Task<List<Tutoria>> GetByUsuarioAsync(int usuarioId);
    Task<List<Tutoria>> GetByHorarioAsync(int horarioId);
    Task<List<Tutoria>> GetByEstadoAsync(string estado);
    Task<List<Tutoria>> GetByFechaAsync(DateTime fecha);
    Task<List<Tutoria>> GetByIdiomaNivelAsync(string idioma, string nivel);
    Task<List<Tutoria>> GetProximasParaRecordatorioAsync(TimeSpan tiempoAntes);
}
