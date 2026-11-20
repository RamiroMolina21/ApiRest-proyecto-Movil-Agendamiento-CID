namespace AgendamientoGestion.Logica.Interfaces;

public enum EstadoCuenta
{
    Activa,
    Desactivada,
    NoExiste
}

/// <summary>Estado de la cuenta y última vez (UTC) que se restablecieron sus credenciales.</summary>
public sealed record InfoCuenta(EstadoCuenta Estado, DateTime? CredencialesCambiadasEn);

/// <summary>
/// Estado de la cuenta del usuario de un token, con caché corta en memoria. Se consulta en cada petición
/// autenticada (JwtBearer OnTokenValidated) para que desactivar, borrar o restablecer la contraseña de una cuenta
/// tenga efecto sin esperar a que caduque el JWT. Los cambios hechos por esta instancia invalidan la entrada al momento.
/// </summary>
public interface IEstadoCuentaCache
{
    Task<EstadoCuenta> ObtenerAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<InfoCuenta> ObtenerInfoAsync(int usuarioId, CancellationToken cancellationToken = default);
    void Invalidar(int usuarioId);
}