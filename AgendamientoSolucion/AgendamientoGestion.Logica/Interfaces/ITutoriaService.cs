using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Seguridad;

namespace AgendamientoGestion.Logica.Interfaces;

/// <summary>
/// Los métodos que reciben <see cref="Solicitante"/> aplican la regla de propiedad de
/// <see cref="ReglasAccesoTutoria"/> (403 si no participa en la tutoría).
/// </summary>
public interface ITutoriaService {
    Task<TutoriaResponseDto> CreateTutoriaAsync(TutoriaCreateDto tutoriaDto, Solicitante solicitante);
    Task<TutoriaResponseDto> GetTutoriaByIdAsync(int id, Solicitante solicitante);
    /// <summary>404 si no existe, 403 si el solicitante no puede realizar <paramref name="accion"/>.</summary>
    Task AsegurarAccesoAsync(int tutoriaId, Solicitante solicitante, AccionTutoria accion);
    Task<List<TutoriaResponseDto>> GetAllTutoriasAsync();
    Task<TutoriaResponseDto> UpdateTutoriaAsync(int id, TutoriaCreateDto tutoriaDto);
    Task<bool> DeleteTutoriaAsync(int id);
    Task<bool> AgregarEstudiantesATutoriaAsync(int tutoriaId, AgregarEstudiantesTutoriaDto estudiantesDto, Solicitante solicitante);
    Task<List<TutoriaResponseDto>> GetCalendarioPorDocenteAsync(string nombre, string apellidos, string correo);
    Task<List<TutoriaResponseDto>> GetCalendarioPorEstudianteAsync(string nombre, string apellidos, string correo);
    Task<List<TutoriaResponseDto>> GetCalendarioPorIdiomaNivelAsync(string idioma, string nivel);
    Task<List<UsuarioResponseDto>> GetEstudiantesByTutoriaAsync(int tutoriaId, Solicitante solicitante);
    Task<bool> EliminarEstudianteDeTutoriaAsync(int tutoriaId, int estudianteId, Solicitante solicitante);
    Task<List<TutoriaResponseDto>> GetTutoriasByEstadoAsync(string estado);
    Task<List<TutoriaResponseDto>> GetTutoriasByEstadoAndUsuarioAsync(string estado, int usuarioId);
    Task<List<TutoriaResponseDto>> GetTutoriasAgendadasPorUsuarioAsync(int usuarioId, string? estado);
    Task<TutoriaResponseDto> FinalizarTutoriaAsync(int tutoriaId, FinalizarTutoriaDto? datos, Solicitante solicitante);
}
