using AgendamientoGestion.Logica.Dtos;

namespace AgendamientoGestion.Logica.Interfaces
{
    public interface IEncuestaService
    {
        Task EnviarEncuestasAsync(int tutoriaId);
        Task<EncuestaResponseDto> ResponderEncuestaAsync(EncuestaCreateDto dto);
        Task<EncuestaResponseDto> VerificarTokenAsync(string token);
        Task<List<EncuestaResponseDto>> GetByTutoriaIdAsync(int tutoriaId);
        Task<EncuestaEstadisticasDto> GetEstadisticasAsync();
    }
}
