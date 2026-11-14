using AgendamientoGestion.Logica.Correo;
using AgendamientoGestion.Logica.Dtos;

namespace AgendamientoGestion.Logica.Interfaces
{
    /// <summary>Correos de tutorías. Devuelven asunto + HTML + texto plano renderizados desde Correo/Plantillas.</summary>
    public interface IEmailTemplateService
    {
        CorreoRenderizado GenerarTemplateNotificacionTutoria(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante);
        CorreoRenderizado GenerarTemplateRecordatorio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante);
        CorreoRenderizado GenerarTemplateCambio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string? motivo, CambioTutoriaDto? cambios);
        CorreoRenderizado GenerarTemplateCancelacion(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string? motivo);
    }
}
