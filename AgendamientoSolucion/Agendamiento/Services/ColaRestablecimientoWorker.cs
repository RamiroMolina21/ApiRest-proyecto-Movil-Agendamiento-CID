using AgendamientoGestion.Logica.Services;

namespace Agendamiento.Services
{
    /// <summary>
    /// Procesa fuera de la petición las solicitudes de "¿Olvidaste tu contraseña?": así la respuesta HTTP tarda lo
    /// mismo exista o no la cuenta (anti-enumeración por tiempo) y un SMTP lento no retrasa al usuario.
    /// La cola es en memoria: si la API se reinicia con solicitudes pendientes, el usuario solo tiene que pedirlo otra vez.
    /// </summary>
    public sealed class ColaRestablecimientoWorker : BackgroundService
    {
        private readonly ColaRestablecimientoContrasena _cola;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ColaRestablecimientoWorker> _logger;

        public ColaRestablecimientoWorker(ColaRestablecimientoContrasena cola, IServiceScopeFactory scopes, ILogger<ColaRestablecimientoWorker> logger)
        {
            _cola = cola;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var correo in _cola.Lector.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IRestablecimientoContrasenaService>()
                        .ProcesarSolicitudAsync(correo, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error procesando una solicitud de restablecimiento de contraseña");
                }
            }
        }
    }
}