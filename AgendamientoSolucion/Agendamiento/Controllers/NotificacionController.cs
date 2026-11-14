using Agendamiento.Security;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = Politicas.GestionWeb)]
    public class NotificacionController : ControllerBase
    {
        private readonly INotificacionService _notificacionService;

        public NotificacionController(INotificacionService notificacionService)
        {
            _notificacionService = notificacionService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotificacion(int id)
        {
            return Ok(await _notificacionService.GetNotificacionByIdAsync(id));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNotificaciones()
        {
            return Ok(await _notificacionService.GetAllNotificacionesAsync());
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> GetNotificacionesByUsuario(int usuarioId)
        {
            return Ok(await _notificacionService.GetNotificacionesByUsuarioAsync(usuarioId));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotificacion(int id)
        {
            if (!await _notificacionService.DeleteNotificacionAsync(id))
            {
                throw new NotFoundException("Notificación no encontrada");
            }

            return Ok(new { message = "Notificación eliminada exitosamente" });
        }

        // NOTA: el endpoint manual POST email/recordatorio/{tutoriaId} sigue deshabilitado: los recordatorios
        // se envían automáticamente 2 horas antes de la tutoría (RecordatorioTutoriaBackgroundService).

        [HttpPost("email/cambio/{tutoriaId}")]
        public async Task<IActionResult> EnviarNotificacionCambioPorEmail(int tutoriaId, [FromBody] string motivo)
        {
            if (!await _notificacionService.EnviarNotificacionCambioPorEmailAsync(tutoriaId, motivo))
            {
                throw new BusinessException("Error al enviar la notificación de cambio por email");
            }

            return Ok(new { message = "Notificación de cambio enviada por email exitosamente" });
        }

        [HttpGet("reporte-excel/{tutoriaId}")]
        public async Task<IActionResult> DescargarReporteExcel(int tutoriaId)
        {
            var (archivoBytes, nombreArchivo) = await _notificacionService.GenerarReporteExcelParaDescargaAsync(tutoriaId);
            return File(archivoBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }
    }
}
