using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Dtos;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace AgendamientoGestion.Logica.Services
{
    public class ExcelService : IExcelService
    {
        public ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public byte[] GenerarReporteTutoria(TutoriaResponseDto tutoria, UsuarioResponseDto docente, List<UsuarioResponseDto> estudiantes, List<RetroalimentacionResponseDto> retroalimentaciones = null, List<EncuestaResponseDto> encuestas = null)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte de Tutoría");

                int row = 1;

                // Título principal
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Value = "REPORTE DE TUTORÍA";
                worksheet.Cells[row, 1].Style.Font.Size = 18;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 30;
                row += 2;

                // Fecha de generación
                worksheet.Cells[row, 1].Value = "Fecha de generación:";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                row += 2;

                // Información de la tutoría - Encabezado de sección
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Value = "INFORMACIÓN DE LA TUTORÍA";
                worksheet.Cells[row, 1].Style.Font.Size = 14;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                // Tabla de información de tutoría
                var tutoriaData = new[]
                {
                    new { Campo = "ID Tutoría", Valor = tutoria.IdTutoria.ToString() },
                    new { Campo = "Idioma", Valor = tutoria.Idioma ?? "N/A" },
                    new { Campo = "Nivel", Valor = tutoria.Nivel ?? "N/A" },
                    new { Campo = "Tema", Valor = tutoria.Tema ?? "N/A" },
                    new { Campo = "Modalidad", Valor = tutoria.Modalidad ?? "N/A" },
                    new { Campo = "Estado", Valor = tutoria.Estado ?? "N/A" },
                    new { Campo = "Fecha y Hora", Valor = tutoria.FechaTutoria.ToString("yyyy-MM-dd HH:mm") },
                    new { Campo = "Espacio", Valor = tutoria.HorarioEspacio ?? "N/A" },
                    new { Campo = "Hora Inicio", Valor = tutoria.HorarioHoraInicio.ToString("HH:mm") },
                    new { Campo = "Hora Fin", Valor = tutoria.HorarioHoraFin.ToString("HH:mm") }
                };

                int startRowTutoria = row;
                foreach (var item in tutoriaData)
                {
                    worksheet.Cells[row, 1].Value = item.Campo;
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 225, 242));
                    worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    worksheet.Cells[row, 2].Value = item.Valor;
                    worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    worksheet.Cells[row, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    row++;
                }

                // Aplicar bordes a la tabla de tutoría
                worksheet.Cells[startRowTutoria, 1, row - 1, 2].Style.Border.BorderAround(ExcelBorderStyle.Medium);
                worksheet.Column(1).Width = 20;
                worksheet.Column(2).Width = 30;
                row += 2;

                // Información del docente - Encabezado de sección
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Value = "INFORMACIÓN DEL DOCENTE";
                worksheet.Cells[row, 1].Style.Font.Size = 14;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                // Tabla de información del docente
                var docenteData = new[]
                {
                    new { Campo = "Nombre", Valor = $"{docente.Nombres} {docente.Apellidos}" },
                    new { Campo = "Correo", Valor = docente.Correo ?? "N/A" }
                };

                int startRowDocente = row;
                foreach (var item in docenteData)
                {
                    worksheet.Cells[row, 1].Value = item.Campo;
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 225, 242));
                    worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    worksheet.Cells[row, 2].Value = item.Valor;
                    worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    worksheet.Cells[row, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    row++;
                }

                // Aplicar bordes a la tabla del docente
                worksheet.Cells[startRowDocente, 1, row - 1, 2].Style.Border.BorderAround(ExcelBorderStyle.Medium);
                row += 2;

                // Estudiantes asignados - Encabezado de sección
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Value = "ESTUDIANTES ASIGNADOS";
                worksheet.Cells[row, 1].Style.Font.Size = 14;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                // Encabezados de la tabla de estudiantes
                worksheet.Cells[row, 1].Value = "Nombre";
                worksheet.Cells[row, 2].Value = "Apellidos";
                worksheet.Cells[row, 3].Value = "Correo";
                worksheet.Cells[row, 1, row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 3].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1, row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 3].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
                worksheet.Cells[row, 1, row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 1, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1, row, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                // Datos de estudiantes
                int startRowEstudiantes = row;
                if (estudiantes != null && estudiantes.Count > 0)
                {
                    foreach (var estudiante in estudiantes)
                    {
                        worksheet.Cells[row, 1].Value = estudiante.Nombres ?? "N/A";
                        worksheet.Cells[row, 2].Value = estudiante.Apellidos ?? "N/A";
                        worksheet.Cells[row, 3].Value = estudiante.Correo ?? "N/A";

                        // Alternar colores de fila para mejor legibilidad
                        var fillColor = row % 2 == 0 ? Color.FromArgb(242, 242, 242) : Color.White;
                        worksheet.Cells[row, 1, row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 1, row, 3].Style.Fill.BackgroundColor.SetColor(fillColor);
                        worksheet.Cells[row, 1, row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[row, 1, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells[row, 1, row, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        row++;
                    }
                }
                else
                {
                    worksheet.Cells[row, 1, row, 3].Merge = true;
                    worksheet.Cells[row, 1].Value = "No hay estudiantes asignados";
                    worksheet.Cells[row, 1].Style.Font.Italic = true;
                    worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.Gray);
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    row++;
                }

                // Aplicar bordes a la tabla de estudiantes
                worksheet.Cells[startRowEstudiantes - 1, 1, row - 1, 3].Style.Border.BorderAround(ExcelBorderStyle.Medium);
                worksheet.Column(3).Width = 35;
                row += 2;

                // Retroalimentaciones - Encabezado de sección
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Value = "RETROALIMENTACIONES";
                worksheet.Cells[row, 1].Style.Font.Size = 14;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                // Encabezados de la tabla de retroalimentaciones
                worksheet.Cells[row, 1].Value = "Fecha";
                worksheet.Cells[row, 2].Value = "Usuario";
                worksheet.Cells[row, 3].Value = "Calificación";
                worksheet.Cells[row, 4].Value = "Comentario";
                worksheet.Cells[row, 1, row, 4].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 4].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
                worksheet.Cells[row, 1, row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 1, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1, row, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                // Datos de retroalimentaciones
                int startRowRetroalimentaciones = row;
                if (retroalimentaciones != null && retroalimentaciones.Count > 0)
                {
                    foreach (var retroalimentacion in retroalimentaciones)
                    {
                        worksheet.Cells[row, 1].Value = retroalimentacion.Fecha.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[row, 2].Value = $"{retroalimentacion.UsuarioNombre} {retroalimentacion.UsuarioApellidos}";
                        worksheet.Cells[row, 3].Value = retroalimentacion.Calificacion;
                        worksheet.Cells[row, 4].Value = retroalimentacion.Comentario ?? "Sin comentario";

                        // Alternar colores de fila para mejor legibilidad
                        var fillColor = row % 2 == 0 ? Color.FromArgb(242, 242, 242) : Color.White;
                        worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(fillColor);
                        worksheet.Cells[row, 1, row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[row, 1, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells[row, 1, row, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        worksheet.Cells[row, 4].Style.WrapText = true; // Permitir que el comentario se ajuste en múltiples líneas

                        row++;
                    }
                }
                else
                {
                    worksheet.Cells[row, 1, row, 4].Merge = true;
                    worksheet.Cells[row, 1].Value = "No hay retroalimentaciones registradas";
                    worksheet.Cells[row, 1].Style.Font.Italic = true;
                    worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.Gray);
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    row++;
                }

                // Aplicar bordes a la tabla de retroalimentaciones
                worksheet.Cells[startRowRetroalimentaciones - 1, 1, row - 1, 4].Style.Border.BorderAround(ExcelBorderStyle.Medium);
                worksheet.Column(1).Width = 20;
                worksheet.Column(2).Width = 30;
                worksheet.Column(3).Width = 15;
                worksheet.Column(4).Width = 50;
                row += 2;

                // ─── ENCUESTAS DE SATISFACCIÓN ───────────────────────────────────
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Value = "ENCUESTAS DE SATISFACCIÓN";
                worksheet.Cells[row, 1].Style.Font.Size = 14;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(84, 130, 53)); // Verde oscuro
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                // Encabezados de la tabla de encuestas
                worksheet.Cells[row, 1].Value = "Estudiante";
                worksheet.Cells[row, 2].Value = "Correo";
                worksheet.Cells[row, 3].Value = "Calificación";
                worksheet.Cells[row, 4].Value = "Comentario";
                worksheet.Cells[row, 1, row, 4].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 4].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(55, 86, 35));
                worksheet.Cells[row, 1, row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 1, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1, row, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(row).Height = 25;
                row++;

                int startRowEncuestas = row;
                var encuestasRespondidas = encuestas?.Where(e => e.TokenUsado).ToList();

                if (encuestasRespondidas != null && encuestasRespondidas.Count > 0)
                {
                    foreach (var enc in encuestasRespondidas)
                    {
                        var estrellas = new string('★', enc.Calificacion) + new string('☆', 5 - enc.Calificacion);

                        worksheet.Cells[row, 1].Value = enc.EstudianteNombre ?? "N/A";
                        worksheet.Cells[row, 2].Value = enc.EstudianteCorreo ?? "N/A";
                        worksheet.Cells[row, 3].Value = $"{enc.Calificacion}/5  {estrellas}";
                        worksheet.Cells[row, 4].Value = enc.Comentario ?? "Sin comentario";

                        // Color de calificación
                        Color ratingColor;
                        if (enc.Calificacion >= 4) ratingColor = Color.FromArgb(198, 239, 206);       // Verde
                        else if (enc.Calificacion == 3) ratingColor = Color.FromArgb(255, 235, 156);  // Amarillo
                        else ratingColor = Color.FromArgb(255, 199, 206);                             // Rojo

                        var fillColorEnc = row % 2 == 0 ? Color.FromArgb(242, 242, 242) : Color.White;
                        worksheet.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(fillColorEnc);
                        worksheet.Cells[row, 3].Style.Fill.BackgroundColor.SetColor(ratingColor);
                        worksheet.Cells[row, 1, row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[row, 1, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells[row, 1, row, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        worksheet.Cells[row, 4].Style.WrapText = true;
                        worksheet.Row(row).Height = 25;
                        row++;
                    }

                    // Fila de promedio
                    row++;
                    double promedio = encuestasRespondidas.Average(e => e.Calificacion);
                    worksheet.Cells[row, 1, row, 2].Merge = true;
                    worksheet.Cells[row, 1].Value = "Promedio de satisfacción:";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    worksheet.Cells[row, 3].Value = $"{promedio:F1}/5";
                    worksheet.Cells[row, 3].Style.Font.Bold = true;
                    worksheet.Cells[row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 3].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 239, 206));
                    row++;
                }
                else
                {
                    worksheet.Cells[row, 1, row, 4].Merge = true;
                    worksheet.Cells[row, 1].Value = encuestas == null || encuestas.Count == 0
                        ? "No se enviaron encuestas para esta tutoría"
                        : "Ningún estudiante ha respondido la encuesta todavía";
                    worksheet.Cells[row, 1].Style.Font.Italic = true;
                    worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.Gray);
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    row++;
                }

                worksheet.Cells[startRowEncuestas - 1, 1, row - 1, 4].Style.Border.BorderAround(ExcelBorderStyle.Medium);

                // Ajustar altura de todas las filas de datos
                for (int i = startRowTutoria; i < row; i++)
                {
                    if (worksheet.Row(i).Height <= 0)
                        worksheet.Row(i).Height = 20;
                }

                return package.GetAsByteArray();
            }
        }

        public string GenerarNombreArchivo(int tutoriaId, string docenteNombre)
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var nombreLimpio = docenteNombre.Replace(" ", "_");
            return $"Reporte_Tutoria_{tutoriaId}_{nombreLimpio}_{fecha}.xlsx";
        }
    }
}
