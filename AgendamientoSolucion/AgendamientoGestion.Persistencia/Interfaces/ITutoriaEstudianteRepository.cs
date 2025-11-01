using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface ITutoriaEstudianteRepository
{
    Task<TutoriaEstudiante> CreateAsync(TutoriaEstudiante tutoriaEstudiante);
    Task<TutoriaEstudiante> GetByIdAsync(int id);
    Task<List<TutoriaEstudiante>> GetAllAsync();
    Task<List<TutoriaEstudiante>> GetByTutoriaAsync(int tutoriaId);
    Task<List<TutoriaEstudiante>> GetByUsuarioAsync(int usuarioId);
    Task<bool> ExistsAsync(int tutoriaId, int usuarioId);
    Task<bool> DeleteAsync(int id);
}

