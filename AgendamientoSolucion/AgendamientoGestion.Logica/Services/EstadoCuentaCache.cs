using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AgendamientoGestion.Logica.Services;

/// <summary>
/// Coste: como máximo una consulta por clave primaria (SELECT activo WHERE idUsuario = @id) por usuario cada
/// <see cref="Duracion"/>; el resto de peticiones se resuelven en memoria. En varias instancias de la API, un
/// cambio hecho en otra instancia tarda como mucho <see cref="Duracion"/> en aplicarse aquí.
/// </summary>
public sealed class EstadoCuentaCache : IEstadoCuentaCache
{
    public static readonly TimeSpan Duracion = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;
    private readonly IUsuarioRepository _usuarioRepository;

    public EstadoCuentaCache(IMemoryCache cache, IUsuarioRepository usuarioRepository)
    {
        _cache = cache;
        _usuarioRepository = usuarioRepository;
    }

    private static string Clave(int usuarioId) => $"estado-cuenta:{usuarioId}";

    public async Task<EstadoCuenta> ObtenerAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(Clave(usuarioId), out EstadoCuenta estado))
            return estado;

        var activo = await _usuarioRepository.GetActivoAsync(usuarioId);
        estado = activo switch
        {
            null => EstadoCuenta.NoExiste,
            true => EstadoCuenta.Activa,
            false => EstadoCuenta.Desactivada
        };
        _cache.Set(Clave(usuarioId), estado, Duracion);
        return estado;
    }

    public void Invalidar(int usuarioId) => _cache.Remove(Clave(usuarioId));
}
