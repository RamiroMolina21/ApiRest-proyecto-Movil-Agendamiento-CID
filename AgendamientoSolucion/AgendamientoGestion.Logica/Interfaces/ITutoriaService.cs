using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces;

public interface ITutoriaService {
    Task<TutoriaResponseDto> CreateTutoriaAsync(TutoriaCreateDto tutoriaDto);
    Task<TutoriaResponseDto> GetTutoriaByIdAsync(int id);
    Task<List<TutoriaResponseDto>> GetAllTutoriasAsync();
    Task<TutoriaResponseDto> UpdateTutoriaAsync(int id, TutoriaCreateDto tutoriaDto);
    Task<bool> DeleteTutoriaAsync(int id);
    Task<bool> AgregarEstudiantesATutoriaAsync(int tutoriaId, AgregarEstudiantesTutoriaDto estudiantesDto);
    Task<List<TutoriaResponseDto>> GetCalendarioPorDocenteAsync(string nombre, string apellidos, string correo);
    Task<List<TutoriaResponseDto>> GetCalendarioPorEstudianteAsync(string nombre, string apellidos, string correo);
    Task<List<TutoriaResponseDto>> GetCalendarioPorIdiomaNivelAsync(string idioma, string nivel);
    Task<List<UsuarioResponseDto>> GetEstudiantesByTutoriaAsync(int tutoriaId);
    Task<bool> EliminarEstudianteDeTutoriaAsync(int tutoriaId, int estudianteId);
    Task<List<TutoriaResponseDto>> GetTutoriasByEstadoAsync(string estado);
    Task<List<TutoriaResponseDto>> GetTutoriasByEstadoAndUsuarioAsync(string estado, int usuarioId);
}
