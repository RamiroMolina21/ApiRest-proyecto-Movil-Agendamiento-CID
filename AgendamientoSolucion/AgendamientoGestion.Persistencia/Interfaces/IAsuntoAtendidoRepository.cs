using AgendamientoGestion.Entidades.Entities;

namespace AgendamientoGestion.Persistencia.Interfaces
{
    public interface IAsuntoAtendidoRepository
    {
        /// <summary>Atendidos (con Usuario) de un tipo, o de todos si <paramref name="tipo"/> es null. Orden: tipo, clave.</summary>
        Task<List<AsuntoAtendido>> GetAsync(string? tipo);

        /// <summary>Atendidos (con Usuario) de un tipo cuyas claves están en <paramref name="claves"/>.</summary>
        Task<List<AsuntoAtendido>> GetAsync(string tipo, IReadOnlyCollection<int> claves);

        /// <summary>
        /// Inserta los que no existan. Si otra petición insertó alguno a la vez (índice único), lo ignora:
        /// el primero en marcarlo se conserva. Devuelve cuántos se insertaron.
        /// </summary>
        Task<int> AgregarFaltantesAsync(IReadOnlyCollection<AsuntoAtendido> nuevos);

        /// <summary>Borra el atendido (tipo, clave). false si no existía.</summary>
        Task<bool> DeleteAsync(string tipo, int clave);
    }
}
