using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Agendamiento.Services
{
    public class RecordatorioTutoriaBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecordatorioTutoriaBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Verificar cada minuto
        private readonly TimeSpan _tiempoAntesTutoria = TimeSpan.FromHours(2); // Recordatorio 2 horas antes

        public RecordatorioTutoriaBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RecordatorioTutoriaBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de recordatorios automáticos iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarRecordatoriosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar recordatorios automáticos");
                }

                // Esperar antes de la siguiente verificación
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Servicio de recordatorios automáticos detenido.");
        }

        private async Task ProcesarRecordatoriosAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var tutoriaRepository = scope.ServiceProvider.GetRequiredService<ITutoriaRepository>();
                var notificacionService = scope.ServiceProvider.GetRequiredService<INotificacionService>();
                var tutoriaEstudianteRepository = scope.ServiceProvider.GetRequiredService<ITutoriaEstudianteRepository>();
                var notificacionRepository = scope.ServiceProvider.GetRequiredService<INotificacionRepository>();

                try
                {
                    // Obtener tutorías que están programadas para dentro de 2 horas
                    var tutoriasProximas = await tutoriaRepository.GetProximasParaRecordatorioAsync(_tiempoAntesTutoria);

                    if (tutoriasProximas == null || tutoriasProximas.Count == 0)
                    {
                        return; // No hay tutorías próximas
                    }

                    _logger.LogInformation($"Se encontraron {tutoriasProximas.Count} tutorías próximas para verificar recordatorios.");

                    foreach (var tutoria in tutoriasProximas)
                    {
                        try
                        {
                            // Verificar si ya se envió el recordatorio para esta tutoría
                            var yaEnviado = await VerificarRecordatorioEnviadoAsync(
                                notificacionRepository,
                                tutoriaEstudianteRepository,
                                tutoria.idTutoria);

                            if (yaEnviado)
                            {
                                _logger.LogDebug($"Recordatorio ya enviado para la tutoría {tutoria.idTutoria}");
                                continue;
                            }

                            // Enviar recordatorio
                            _logger.LogInformation($"Enviando recordatorio automático para la tutoría {tutoria.idTutoria}");
                            var enviado = await notificacionService.EnviarRecordatorioPorEmailAsync(tutoria.idTutoria);

                            if (enviado)
                            {
                                _logger.LogInformation($"Recordatorio enviado exitosamente para la tutoría {tutoria.idTutoria}");
                            }
                            else
                            {
                                _logger.LogWarning($"No se pudo enviar el recordatorio para la tutoría {tutoria.idTutoria}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error al procesar recordatorio para la tutoría {tutoria.idTutoria}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener tutorías próximas para recordatorios");
                }
            }
        }

        private async Task<bool> VerificarRecordatorioEnviadoAsync(
            INotificacionRepository notificacionRepository,
            ITutoriaEstudianteRepository tutoriaEstudianteRepository,
            int tutoriaId)
        {
            try
            {
                // Obtener estudiantes de la tutoría
                var tutoriaEstudiantes = await tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);
                if (tutoriaEstudiantes == null || tutoriaEstudiantes.Count == 0)
                {
                    return false; // No hay estudiantes, no se puede enviar recordatorio
                }

                var estudianteIds = tutoriaEstudiantes.Select(te => te.Usuario_idUsuario).ToList();

                // Obtener todas las notificaciones de los estudiantes de las últimas 24 horas
                var fechaLimite = DateTime.Now.AddHours(-24);
                var notificacionesRecientes = await notificacionRepository.GetByFechaAsync(fechaLimite);

                if (notificacionesRecientes == null || notificacionesRecientes.Count == 0)
                {
                    return false;
                }

                // Verificar si hay alguna notificación de recordatorio para algún estudiante de esta tutoría
                foreach (var notificacion in notificacionesRecientes)
                {
                    if (estudianteIds.Contains(notificacion.Usuario_idUsuario) &&
                        notificacion.asunto != null &&
                        notificacion.asunto.Contains("Recordatorio", StringComparison.OrdinalIgnoreCase))
                    {
                        // Verificar que sea de las últimas 24 horas (ya está filtrado pero verificamos por seguridad)
                        if (notificacion.fecha >= fechaLimite)
                        {
                            return true; // Ya se envió un recordatorio para esta tutoría
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si se envió recordatorio para tutoría {tutoriaId}");
                return false; // En caso de error, permitir enviar para evitar perder recordatorios
            }
        }
    }
}

