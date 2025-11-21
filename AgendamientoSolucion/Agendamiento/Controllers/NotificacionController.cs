using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            try
            {
                var notificacion = await _notificacionService.GetNotificacionByIdAsync(id);
                return Ok(notificacion);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNotificaciones()
        {
            try
            {
                var notificaciones = await _notificacionService.GetAllNotificacionesAsync();
                return Ok(notificaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> GetNotificacionesByUsuario(int usuarioId)
        {
            try
            {
                var notificaciones = await _notificacionService.GetNotificacionesByUsuarioAsync(usuarioId);
                return Ok(notificaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotificacion(int id)
        {
            try
            {
                var result = await _notificacionService.DeleteNotificacionAsync(id);
                if (result)
                {
                    return Ok(new { message = "Notificación eliminada exitosamente" });
                }
                return NotFound(new { message = "Notificación no encontrada" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // Nuevos endpoints para envío por email
        // NOTA: El endpoint de recordatorio manual ha sido deshabilitado.
        // Los recordatorios ahora se envían automáticamente 2 horas antes de la tutoría mediante un Background Service.
        /*
        [HttpPost("email/recordatorio/{tutoriaId}")]
        [Obsolete("Los recordatorios ahora se envían automáticamente. Este endpoint está deshabilitado.")]
        public async Task<IActionResult> EnviarRecordatorioPorEmail(int tutoriaId)
        {
            try
            {
                var result = await _notificacionService.EnviarRecordatorioPorEmailAsync(tutoriaId);
                if (result)
                {
                    return Ok(new { message = "Recordatorio enviado por email exitosamente" });
                }
                return BadRequest(new { message = "Error al enviar el recordatorio por email" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
        */

        [HttpPost("email/cambio/{tutoriaId}")]
        public async Task<IActionResult> EnviarNotificacionCambioPorEmail(int tutoriaId, [FromBody] string motivo)
        {
            try
            {
                var result = await _notificacionService.EnviarNotificacionCambioPorEmailAsync(tutoriaId, motivo);
                if (result)
                {
                    return Ok(new { message = "Notificación de cambio enviada por email exitosamente" });
                }
                return BadRequest(new { message = "Error al enviar la notificación de cambio por email" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("reporte-excel/{tutoriaId}")]
        public async Task<IActionResult> DescargarReporteExcel(int tutoriaId)
        {
            try
            {
                var (archivoBytes, nombreArchivo) = await _notificacionService.GenerarReporteExcelParaDescargaAsync(tutoriaId);
                
                return File(archivoBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}
