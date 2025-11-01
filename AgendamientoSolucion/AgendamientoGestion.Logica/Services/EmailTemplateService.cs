using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Dtos;
using System.Collections.Generic;

namespace AgendamientoGestion.Logica.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public string GenerarTemplateNotificacionTutoria(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Notificación de Tutoría</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; border-radius: 5px; margin-bottom: 20px; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #ecf0f1; padding: 15px; border-radius: 5px; margin: 10px 0; }}
        .button {{ background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px 5px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📚 Nueva Tutoría Asignada</h1>
        </div>
        <div class='content'>
            <p>Hola <strong>{estudiante.Nombres} {estudiante.Apellidos}</strong>,</p>
            <p>Te informamos que has sido asignado a una nueva tutoría. Aquí están los detalles:</p>
            
            <div class='info-box'>
                <h3>📋 Información de la Tutoría</h3>
                <p><strong>ID:</strong> {tutoria.IdTutoria}</p>
                <p><strong>Idioma:</strong> {tutoria.Idioma}</p>
                <p><strong>Nivel:</strong> {tutoria.Nivel}</p>
                <p><strong>Tema:</strong> {tutoria.Tema}</p>
                <p><strong>Modalidad:</strong> {tutoria.Modalidad}</p>
                <p><strong>Fecha y Hora:</strong> {tutoria.FechaTutoria:dd/MM/yyyy HH:mm}</p>
                <p><strong>Espacio:</strong> {tutoria.HorarioEspacio}</p>
            </div>
            
            <p>Por favor, confirma tu asistencia respondiendo a este correo.</p>
            
            <div style='text-align: center; margin: 20px 0;'>
                <a href='mailto:sistema@universidad.edu.co?subject=Confirmación Tutoría {tutoria.IdTutoria}&body=Confirmo mi asistencia a la tutoría' class='button'>✅ Confirmar Asistencia</a>
                <a href='mailto:sistema@universidad.edu.co?subject=Cancelación Tutoría {tutoria.IdTutoria}&body=Necesito cancelar mi participación en la tutoría' class='button' style='background-color: #e74c3c;'>❌ Cancelar</a>
            </div>
        </div>
        <div class='footer'>
            <p>Sistema de Agendamiento de Tutorías - CECAR</p>
            <p>Este es un correo automático, por favor no responder directamente.</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GenerarTemplateRecordatorio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Recordatorio de Tutoría</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #f39c12; color: white; padding: 20px; text-align: center; border-radius: 5px; margin-bottom: 20px; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #fef9e7; padding: 15px; border-radius: 5px; margin: 10px 0; border-left: 4px solid #f39c12; }}
        .footer {{ text-align: center; margin-top: 30px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⏰ Recordatorio de Tutoría</h1>
        </div>
        <div class='content'>
            <p>Hola <strong>{estudiante.Nombres} {estudiante.Apellidos}</strong>,</p>
            <p>Te recordamos que tienes una tutoría programada:</p>
            
            <div class='info-box'>
                <h3>📅 Próxima Tutoría</h3>
                <p><strong>Fecha y Hora:</strong> {tutoria.FechaTutoria:dd/MM/yyyy HH:mm}</p>
                <p><strong>Idioma:</strong> {tutoria.Idioma} - {tutoria.Nivel}</p>
                <p><strong>Tema:</strong> {tutoria.Tema}</p>
                <p><strong>Modalidad:</strong> {tutoria.Modalidad}</p>
                <p><strong>Espacio:</strong> {tutoria.HorarioEspacio}</p>
            </div>
            
            <p><strong>¡No olvides asistir puntualmente!</strong></p>
        </div>
        <div class='footer'>
            <p>Sistema de Agendamiento de Tutorías - CECAR</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GenerarTemplateCambio(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string motivo, CambioTutoriaDto cambios)
        {
            var listaCambios = cambios?.ObtenerListaCambios() ?? new List<string>();
            var cambiosHtml = "";

            if (listaCambios.Count > 0)
            {
                cambiosHtml = "<h3>🔍 Cambios Realizados:</h3><ul style='list-style-type: none; padding-left: 0;'>";
                foreach (var cambio in listaCambios)
                {
                    cambiosHtml += $"<li style='padding: 8px; margin: 5px 0; background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 3px;'>🔸 {cambio}</li>";
                }
                cambiosHtml += "</ul>";
            }
            else
            {
                cambiosHtml = "<p><em>No se detectaron cambios específicos en los campos principales.</em></p>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Cambio en Tutoría</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #e74c3c; color: white; padding: 20px; text-align: center; border-radius: 5px; margin-bottom: 20px; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #fdf2f2; padding: 15px; border-radius: 5px; margin: 10px 0; border-left: 4px solid #e74c3c; }}
        .cambios-box {{ background-color: #fffbf0; padding: 15px; border-radius: 5px; margin: 10px 0; border-left: 4px solid #ffc107; }}
        .footer {{ text-align: center; margin-top: 30px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Cambio en Tutoría</h1>
        </div>
        <div class='content'>
            <p>Hola <strong>{estudiante.Nombres} {estudiante.Apellidos}</strong>,</p>
            <p>Te informamos que ha habido un cambio en tu tutoría programada:</p>
            
            <div class='cambios-box'>
                {cambiosHtml}
            </div>

            <div class='info-box'>
                <h3>📋 Información Actualizada</h3>
                <p><strong>ID Tutoría:</strong> {tutoria.IdTutoria}</p>
                <p><strong>Fecha y Hora:</strong> {tutoria.FechaTutoria:dd/MM/yyyy HH:mm}</p>
                <p><strong>Idioma:</strong> {tutoria.Idioma} - {tutoria.Nivel}</p>
                <p><strong>Tema:</strong> {tutoria.Tema}</p>
                <p><strong>Modalidad:</strong> {tutoria.Modalidad}</p>
                <p><strong>Espacio/Aula:</strong> {tutoria.HorarioEspacio}</p>
                <p><strong>Profesor/Tutor:</strong> {tutoria.UsuarioNombre} {tutoria.UsuarioApellidos}</p>
                {(string.IsNullOrEmpty(motivo) ? "" : $"<p><strong>Motivo del cambio:</strong> {motivo}</p>")}
            </div>
            
            <p>Por favor, toma nota de los cambios y confirma tu disponibilidad.</p>
        </div>
        <div class='footer'>
            <p>Sistema de Agendamiento de Tutorías - CECAR</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GenerarTemplateCancelacion(TutoriaResponseDto tutoria, UsuarioResponseDto estudiante, string motivo)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Cancelación de Tutoría</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #95a5a6; color: white; padding: 20px; text-align: center; border-radius: 5px; margin-bottom: 20px; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 10px 0; border-left: 4px solid #95a5a6; }}
        .footer {{ text-align: center; margin-top: 30px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Tutoría Cancelada</h1>
        </div>
        <div class='content'>
            <p>Hola <strong>{estudiante.Nombres} {estudiante.Apellidos}</strong>,</p>
            <p>Te informamos que la siguiente tutoría ha sido cancelada:</p>
            
            <div class='info-box'>
                <h3>📋 Tutoría Cancelada</h3>
                <p><strong>ID:</strong> {tutoria.IdTutoria}</p>
                <p><strong>Fecha y Hora:</strong> {tutoria.FechaTutoria:dd/MM/yyyy HH:mm}</p>
                <p><strong>Idioma:</strong> {tutoria.Idioma} - {tutoria.Nivel}</p>
                <p><strong>Tema:</strong> {tutoria.Tema}</p>
                <p><strong>Motivo de cancelación:</strong> {motivo}</p>
            </div>
            
            <p>Si tienes alguna pregunta, por favor contacta con el sistema de tutorías.</p>
        </div>
        <div class='footer'>
            <p>Sistema de Agendamiento de Tutorías - CECAR</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
