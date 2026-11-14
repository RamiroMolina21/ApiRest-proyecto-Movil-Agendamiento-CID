using AgendamientoGestion.Logica.Dtos;

namespace AgendamientoGestion.Logica.Interfaces
{
    public interface IEncuestaService
    {
        /// <summary>
        /// Crea y envía encuestas a los estudiantes de la tutoría. Idempotente: no crea una segunda encuesta
        /// para un estudiante (por correo) que ya tiene una en esa tutoría.
        /// </summary>
        Task<EnvioEncuestasResultadoDto> EnviarEncuestasAsync(int tutoriaId);
        Task<EncuestaResponseDto> ResponderEncuestaAsync(EncuestaCreateDto dto);
        Task<EncuestaResponseDto> VerificarTokenAsync(string token);
        Task<List<EncuestaResponseDto>> GetByTutoriaIdAsync(int tutoriaId);
        Task<EncuestaEstadisticasDto> GetEstadisticasAsync();

        /// <summary>Tutorías con encuestas enviadas sin responder (clave del asunto "encuestas-sin-responder").</summary>
        Task<List<EncuestasPendientesDto>> GetPendientesAsync();
    }
}
