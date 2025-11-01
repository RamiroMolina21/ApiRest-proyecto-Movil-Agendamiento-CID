using AgendamientoGestion.Logica.Dtos;

namespace AgendamientoGestion.Logica.Interfaces
{
    public interface IExcelService
    {
        byte[] GenerarReporteTutoria(TutoriaResponseDto tutoria, UsuarioResponseDto docente, List<UsuarioResponseDto> estudiantes);
        string GenerarNombreArchivo(int tutoriaId, string docenteNombre);
    }
}
