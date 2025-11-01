using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Dtos;
using System.Data;
using System.Text;

namespace AgendamientoGestion.Logica.Services
{
    public class ExcelService : IExcelService
    {
        public byte[] GenerarReporteTutoria(TutoriaResponseDto tutoria, UsuarioResponseDto docente, List<UsuarioResponseDto> estudiantes)
        {
            var csv = new StringBuilder();
            
            // Encabezados
            csv.AppendLine("REPORTE DE TUTORÍA");
            csv.AppendLine($"Fecha de generación: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();
            
            // Información de la tutoría
            csv.AppendLine("INFORMACIÓN DE LA TUTORÍA");
            csv.AppendLine($"ID Tutoría: {tutoria.IdTutoria}");
            csv.AppendLine($"Idioma: {tutoria.Idioma}");
            csv.AppendLine($"Nivel: {tutoria.Nivel}");
            csv.AppendLine($"Tema: {tutoria.Tema}");
            csv.AppendLine($"Modalidad: {tutoria.Modalidad}");
            csv.AppendLine($"Estado: {tutoria.Estado}");
            csv.AppendLine($"Fecha y Hora: {tutoria.FechaTutoria:yyyy-MM-dd HH:mm}");
            csv.AppendLine($"Espacio: {tutoria.HorarioEspacio}");
            csv.AppendLine($"Hora Inicio: {tutoria.HorarioHoraInicio:HH:mm}");
            csv.AppendLine($"Hora Fin: {tutoria.HorarioHoraFin:HH:mm}");
            csv.AppendLine();
            
            // Información del docente
            csv.AppendLine("INFORMACIÓN DEL DOCENTE");
            csv.AppendLine($"Nombre: {docente.Nombres} {docente.Apellidos}");
            csv.AppendLine($"Correo: {docente.Correo}");
            csv.AppendLine();
            
            // Lista de estudiantes
            csv.AppendLine("ESTUDIANTES ASIGNADOS");
            csv.AppendLine("Nombre,Apellidos,Correo");
            foreach (var estudiante in estudiantes)
            {
                csv.AppendLine($"{estudiante.Nombres},{estudiante.Apellidos},{estudiante.Correo}");
            }
            
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public string GenerarNombreArchivo(int tutoriaId, string docenteNombre)
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var nombreLimpio = docenteNombre.Replace(" ", "_");
            return $"Reporte_Tutoria_{tutoriaId}_{nombreLimpio}_{fecha}.csv";
        }
    }
}
