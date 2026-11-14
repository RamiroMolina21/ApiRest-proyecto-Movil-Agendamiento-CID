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
    /// <summary>Inserta todas las asignaciones en un único SaveChanges (atómico: todas o ninguna).</summary>
    Task CreateRangeAsync(IEnumerable<TutoriaEstudiante> tutoriaEstudiantes);
    Task<TutoriaEstudiante> GetByIdAsync(int id);
    Task<List<TutoriaEstudiante>> GetAllAsync();
    Task<List<TutoriaEstudiante>> GetByTutoriaAsync(int tutoriaId);
    /// <summary>Inscripciones (con Usuario y Rol, sin seguimiento) de varias tutorías en UNA consulta (evita N+1).</summary>
    Task<List<TutoriaEstudiante>> GetByTutoriasAsync(IReadOnlyCollection<int> tutoriaIds);
    Task<List<TutoriaEstudiante>> GetByUsuarioAsync(int usuarioId);
    Task<bool> ExistsAsync(int tutoriaId, int usuarioId);
    Task<int> CountByTutoriaAsync(int tutoriaId);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByTutoriaAndEstudianteAsync(int tutoriaId, int estudianteId);
}

