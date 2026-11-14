using AgendamientoGestion.Logica.Correo;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Interfaces;

namespace AgendamientoGestion.Logica.Services
{
    /// <summary>
    /// Adaptador DTO → modelo tipado de correo. El HTML ya no vive aquí: está en Correo/Plantillas (ver docs/correo).
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IPlantillasCorreo _plantillas;

        public EmailTemplateService(IPlantillasCorreo plantillas)
        {
            _plantillas = plantillas;
        }

        private static string Nombre(UsuarioResponseDto u) => $"{u.Nombres} {u.Apellidos}".Trim();

        public CorreoRenderizado GenerarTemplateNotificacionTutoria(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante) =>
            _plantillas.Renderizar(new NuevaTutoriaCorreo(
                Nombre(estudiante), tutoria.IdTutoria, tutoria.Idioma, tutoria.Nivel, tutoria.Tema, tutoria.Modalidad,
                tutoria.FechaTutoria, tutoria.HorarioEspacio));

        public CorreoRenderizado GenerarTemplateRecordatorio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante) =>
            _plantillas.Renderizar(new RecordatorioTutoriaCorreo(
                Nombre(estudiante), tutoria.FechaTutoria, tutoria.Idioma, tutoria.Nivel, tutoria.Tema, tutoria.Modalidad,
                tutoria.HorarioEspacio));

        public CorreoRenderizado GenerarTemplateCambio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string? motivo, CambioTutoriaDto? cambios) =>
            _plantillas.Renderizar(new CambioTutoriaCorreo(
                Nombre(estudiante), tutoria.IdTutoria, tutoria.FechaTutoria, tutoria.Idioma, tutoria.Nivel, tutoria.Tema,
                tutoria.Modalidad, tutoria.HorarioEspacio,
                $"{tutoria.UsuarioNombre} {tutoria.UsuarioApellidos}".Trim(),
                motivo, cambios?.ObtenerListaCambios() ?? new List<string>()));

        public CorreoRenderizado GenerarTemplateCancelacion(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string? motivo) =>
            _plantillas.Renderizar(new CancelacionTutoriaCorreo(
                Nombre(estudiante), tutoria.IdTutoria, tutoria.FechaTutoria, tutoria.Idioma, tutoria.Nivel, tutoria.Tema, motivo));
    }
}
