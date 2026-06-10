using AgendamientoGestion.Entidades.Entities;

namespace AgendamientoGestion.Persistencia.Interfaces
{
    public interface IEncuestaRepository
    {
        Task<EncuestaSatisfaccion> CreateAsync(EncuestaSatisfaccion encuesta);
        Task<EncuestaSatisfaccion> GetByIdAsync(int id);
        Task<EncuestaSatisfaccion> GetByTokenAsync(string token);
        Task<List<EncuestaSatisfaccion>> GetByTutoriaIdAsync(int tutoriaId);
        Task<List<EncuestaSatisfaccion>> GetAllAsync();
        Task<EncuestaSatisfaccion> UpdateAsync(EncuestaSatisfaccion encuesta);
    }
}
