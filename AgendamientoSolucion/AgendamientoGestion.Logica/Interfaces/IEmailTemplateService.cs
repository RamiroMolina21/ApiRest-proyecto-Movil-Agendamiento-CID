using AgendamientoGestion.Logica.Dtos;

namespace AgendamientoGestion.Logica.Interfaces
{
    public interface IEmailTemplateService
    {
        string GenerarTemplateNotificacionTutoria(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante);
        string GenerarTemplateRecordatorio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante);
        string GenerarTemplateCambio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string motivo, CambioTutoriaDto cambios);
        string GenerarTemplateCancelacion(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string motivo);
    }
}
