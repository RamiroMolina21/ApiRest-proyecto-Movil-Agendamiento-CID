using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces
{
    public interface INotificacionService
    {
        Task<NotificacionResponseDto> CreateNotificacionAsync(NotificacionCreateDto notificacionDto);
        Task<NotificacionResponseDto> GetNotificacionByIdAsync(int id);
        Task<List<NotificacionResponseDto>> GetAllNotificacionesAsync();
        Task<NotificacionResponseDto> UpdateNotificacionAsync(int id, NotificacionCreateDto notificacionDto);
        Task<bool> DeleteNotificacionAsync(int id);
        Task<List<NotificacionResponseDto>> GetNotificacionesByUsuarioAsync(int usuarioId);
        Task<bool> EnviarNotificacionTutoriaAsync(int tutoriaId, string tipoNotificacion);
        Task<bool> EnviarRecordatorioAsync(int tutoriaId);
        Task<bool> EnviarNotificacionCambioAsync(int tutoriaId, string motivo);
        
        // Nuevos métodos para envío por email
        Task<bool> EnviarNotificacionTutoriaPorEmailAsync(int tutoriaId, string tipoNotificacion);
        Task<bool> EnviarRecordatorioPorEmailAsync(int tutoriaId);
        Task<bool> EnviarNotificacionCambioPorEmailAsync(int tutoriaId, string motivo, TutoriaResponseDto tutoriaAnterior = null);
        Task<bool> EnviarNotificacionEstudianteTutoriaAsync(TutoriaResponseDto tutoriaDto, UsuarioResponseDto estudianteDto, string tipoNotificacion);
        Task<(byte[] archivoBytes, string nombreArchivo)> GenerarReporteExcelParaDescargaAsync(int tutoriaId, int docenteId);
    }
}
