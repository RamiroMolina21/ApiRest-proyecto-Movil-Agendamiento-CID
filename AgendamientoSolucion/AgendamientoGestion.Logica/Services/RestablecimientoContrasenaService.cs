using AgendamientoGestion.Entidades.Constants;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Correo;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;
using AgendamientoGestion.Logica.Seguridad;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace AgendamientoGestion.Logica.Services;

public sealed record ValidacionTokenDto(bool Valido, string Correo, DateTime ExpiraEn);

public interface IRestablecimientoContrasenaService
{
    /// <summary>
    /// Encola la solicitud y vuelve de inmediato (misma respuesta y mismo tiempo exista o no la cuenta).
    /// 400 si el correo no es válido; 429 si se supera el límite por correo.
    /// </summary>
    void Solicitar(string? correo);

    /// <summary>Trabajo en segundo plano: crea el token y envía el correo SOLO a cuentas activas con acceso.</summary>
    Task ProcesarSolicitudAsync(string correo, CancellationToken ct = default);

    /// <summary>
    /// SOLO para el endpoint de desarrollo /dev/restablecimiento/enlace (e2e): crea un token para una cuenta con
    /// acceso, activa y cuyo nombre empieza por "[PRUEBA]", sin enviar correo. null si no cumple.
    /// </summary>
    Task<string?> GenerarEnlacePruebaAsync(string? correo);

    /// <summary>400 si el token no es válido, ha caducado o ya se usó.</summary>
    Task<ValidacionTokenDto> ValidarAsync(string? token);

    /// <summary>Cambia la contraseña con un token válido (un solo uso). 400 token o política.</summary>
    Task RestablecerAsync(string? token, string? contrasenaNueva);
}

/// <summary>Cola en memoria de correos que pidieron restablecimiento (la procesa ColaRestablecimientoWorker).</summary>
public sealed class ColaRestablecimientoContrasena
{
    private readonly Channel<string> _canal = Channel.CreateBounded<string>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public bool Encolar(string correo) => _canal.Writer.TryWrite(correo);
    public ChannelReader<string> Lector => _canal.Reader;
}

/// <summary>
/// "¿Olvidaste tu contraseña?". Token de 32 bytes aleatorios (RandomNumberGenerator) en base64url; en BD solo su
/// SHA-256. Un solo uso, caduca (RestablecimientoContrasena:MinutosValidez). El token nunca se registra en logs.
/// </summary>
public sealed class RestablecimientoContrasenaService : IRestablecimientoContrasenaService
{
    public const string MensajeSolicitud = "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.";
    public const string MensajeTokenInvalido = "El enlace no es válido o ha caducado. Solicita uno nuevo.";
    public const string MensajeDemasiadas = "Demasiadas solicitudes. Espera unos minutos e inténtalo de nuevo.";
    public const string RutaWeb = "restablecer-contrasena";

    private readonly IUsuarioRepository _usuarios;
    private readonly IRestablecimientoContrasenaRepository _repositorio;
    private readonly IPlantillasCorreo _plantillas;
    private readonly ICorreoService _correo;
    private readonly IEstadoCuentaCache _estadoCuenta;
    private readonly ColaRestablecimientoContrasena _cola;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _reloj;
    private readonly RestablecimientoSettings _opciones;
    private readonly WebSettings _web;
    private readonly bool _registrarEnlaceEnLog;
    private readonly ILogger<RestablecimientoContrasenaService> _logger;

    public RestablecimientoContrasenaService(
        IUsuarioRepository usuarios,
        IRestablecimientoContrasenaRepository repositorio,
        IPlantillasCorreo plantillas,
        ICorreoService correo,
        IEstadoCuentaCache estadoCuenta,
        ColaRestablecimientoContrasena cola,
        IMemoryCache cache,
        TimeProvider reloj,
        IOptions<RestablecimientoSettings> opciones,
        IOptions<WebSettings> web,
        RegistroEnlaceDesarrollo registroEnlace,
        ILogger<RestablecimientoContrasenaService> logger)
    {
        _usuarios = usuarios;
        _repositorio = repositorio;
        _plantillas = plantillas;
        _correo = correo;
        _estadoCuenta = estadoCuenta;
        _cola = cola;
        _cache = cache;
        _reloj = reloj;
        _opciones = opciones.Value;
        _web = web.Value;
        _registrarEnlaceEnLog = registroEnlace.Habilitado;
        _logger = logger;
    }

    public void Solicitar(string? correo)
    {
        EmailValidator.ValidateEmail(correo ?? string.Empty, "Correo");
        var normalizado = correo!.Trim().ToLowerInvariant();

        // Límite por correo (exista o no la cuenta: no revela nada). El de IP lo aplica el rate limiter.
        var clave = $"olvide-contrasena:{normalizado}";
        var ahora = _reloj.GetUtcNow();
        var ventana = _cache.GetOrCreate(clave, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_opciones.VentanaSegundos);
            return new VentanaSolicitudes(ahora.AddSeconds(_opciones.VentanaSegundos));
        })!;
        if (Interlocked.Increment(ref ventana.Contador) > _opciones.SolicitudesPorCorreo)
            throw new DemasiadasSolicitudesException(MensajeDemasiadas, (int)Math.Ceiling((ventana.Fin - ahora).TotalSeconds));

        if (!_cola.Encolar(normalizado))
            _logger.LogWarning("Cola de restablecimiento de contraseña llena: solicitud descartada");
    }

    private sealed class VentanaSolicitudes
    {
        public VentanaSolicitudes(DateTimeOffset fin) => Fin = fin;
        public DateTimeOffset Fin { get; }
        public int Contador;
    }

    public async Task ProcesarSolicitudAsync(string correo, CancellationToken ct = default)
    {
        var usuario = await _usuarios.GetByEmailAsync(correo);
        // Estudiantes (no inician sesión), cuentas desactivadas o inexistentes: no se envía nada.
        if (usuario == null || !usuario.activo || Roles.NormalizarRolConAcceso(usuario.Rol?.tipoRol) == null)
        {
            _logger.LogInformation("Solicitud de restablecimiento sin envío (cuenta inexistente, sin acceso o desactivada)");
            return;
        }

        var enlace = await CrearEnlaceAsync(usuario);

        if (_registrarEnlaceEnLog)
            _logger.LogWarning("[SOLO DESARROLLO] Enlace de restablecimiento para el usuario {UsuarioId}: {Enlace}", usuario.idUsuario, enlace);

        var contenido = _plantillas.Renderizar(new RestablecerContrasenaCorreo(
            $"{usuario.nombres} {usuario.apellidos}".Trim(), usuario.correo, enlace, _opciones.MinutosValidez));
        try
        {
            _correo.Enviar(MensajeCorreo.Desde(contenido, usuario.correo, $"{usuario.nombres} {usuario.apellidos}".Trim()));
            _logger.LogInformation("Enlace de restablecimiento enviado al usuario {UsuarioId}", usuario.idUsuario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el enlace de restablecimiento al usuario {UsuarioId}", usuario.idUsuario);
        }
    }

    public const string PrefijoPrueba = "[PRUEBA]";

    public async Task<string?> GenerarEnlacePruebaAsync(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return null;
        var usuario = await _usuarios.GetByEmailAsync(correo.Trim());
        if (usuario == null || !usuario.activo || Roles.NormalizarRolConAcceso(usuario.Rol?.tipoRol) == null
            || !usuario.nombres.StartsWith(PrefijoPrueba, StringComparison.Ordinal))
            return null;
        return await CrearEnlaceAsync(usuario);
    }

    private async Task<string> CrearEnlaceAsync(Usuario usuario)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        await _repositorio.CrearInvalidandoAnterioresAsync(new RestablecimientoContrasena
        {
            Usuario_idUsuario = usuario.idUsuario,
            tokenHash = Hash(token),
            fechaCreacion = ahora,
            fechaExpiracion = ahora.AddMinutes(_opciones.MinutosValidez)
        }, ahora);
        return _web.Enlace(RutaWeb, "token", token);
    }

    public async Task<ValidacionTokenDto> ValidarAsync(string? token)
    {
        var solicitud = await BuscarVigenteAsync(token);
        return new ValidacionTokenDto(true, Enmascarar(solicitud.Usuario.correo),
            DateTime.SpecifyKind(solicitud.fechaExpiracion, DateTimeKind.Utc));
    }

    public async Task RestablecerAsync(string? token, string? contrasenaNueva)
    {
        var solicitud = await BuscarVigenteAsync(token);
        var usuario = await _usuarios.GetByIdAsync(solicitud.Usuario_idUsuario)
                      ?? throw new ValidationException(MensajeTokenInvalido);
        if (!usuario.activo || Roles.NormalizarRolConAcceso(usuario.Rol?.tipoRol) == null)
            throw new ValidationException(MensajeTokenInvalido);

        PoliticaContrasena.Validar(contrasenaNueva, usuario.correo, n => VerificarSinExcepcion(n, usuario.contrasenaHash));

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        // Un solo uso: si otra petición lo consumió a la vez, esta pierde.
        if (!await _repositorio.ConsumirAsync(solicitud.idRestablecimiento, usuario.idUsuario, ahora))
            throw new ValidationException(MensajeTokenInvalido);

        usuario.contrasenaHash = BCrypt.Net.BCrypt.HashPassword(contrasenaNueva);
        usuario.credencialesCambiadasEn = ahora; // invalida los JWT emitidos antes (ValidacionCuentaJwt)
        await _usuarios.UpdateAsync(usuario);
        _estadoCuenta.Invalidar(usuario.idUsuario);
        _logger.LogInformation("El usuario {UsuarioId} restableció su contraseña con un enlace", usuario.idUsuario);

        try
        {
            var aviso = _plantillas.Renderizar(new ContrasenaCambiadaCorreo(
                $"{usuario.nombres} {usuario.apellidos}".Trim(), usuario.correo, HoraColombia(ahora)));
            _correo.Enviar(MensajeCorreo.Desde(aviso, usuario.correo, $"{usuario.nombres} {usuario.apellidos}".Trim()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el aviso de contraseña cambiada al usuario {UsuarioId}", usuario.idUsuario);
        }
    }

    private async Task<RestablecimientoContrasena> BuscarVigenteAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128)
            throw new ValidationException(MensajeTokenInvalido);
        return await _repositorio.GetVigentePorHashAsync(Hash(token.Trim()), _reloj.GetUtcNow().UtcDateTime)
               ?? throw new ValidationException(MensajeTokenInvalido);
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>"ramiro.admin@cecar.edu.co" → "r***@cecar.edu.co".</summary>
    public static string Enmascarar(string correo)
    {
        var i = correo.IndexOf('@');
        return i <= 0 ? "***" : $"{correo[0]}***{correo[i..]}";
    }

    private static bool VerificarSinExcepcion(string texto, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(texto, hash); }
        catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException) { return false; }
    }

    private static DateTime HoraColombia(DateTime utc)
    {
        try { return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("America/Bogota")); }
        catch (TimeZoneNotFoundException) { return utc.AddHours(-5); } // Colombia no tiene horario de verano
    }
}

/// <summary>Si el enlace se escribe en el log (solo puede ser true en Development: lo decide Program.cs).</summary>
public sealed record RegistroEnlaceDesarrollo(bool Habilitado);
