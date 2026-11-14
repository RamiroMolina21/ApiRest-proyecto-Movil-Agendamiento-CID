namespace AgendamientoGestion.Logica.Interfaces;

public enum EstadoCuenta
{
    Activa,
    Desactivada,
    NoExiste
}

/// <summary>
/// Estado de la cuenta del usuario de un token, con caché corta en memoria. Se consulta en cada petición
/// autenticada (JwtBearer OnTokenValidated) para que desactivar o borrar una cuenta tenga efecto sin esperar
/// a que caduque el JWT. Los cambios hechos por esta instancia invalidan la entrada al momento.
/// </summary>
public interface IEstadoCuentaCache
{
    Task<EstadoCuenta> ObtenerAsync(int usuarioId, CancellationToken cancellationToken = default);
    void Invalidar(int usuarioId);
}
