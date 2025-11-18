using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Services;

public class NotificacionService : INotificacionService
{
    private readonly INotificacionRepository _notificacionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITutoriaRepository _tutoriaRepository;
    private readonly ITutoriaEstudianteRepository _tutoriaEstudianteRepository;
    private readonly IHorarioRepository _horarioRepository;
    private readonly ICorreoService _correoService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IExcelService _excelService;

    public NotificacionService(
        INotificacionRepository notificacionRepository, 
        IUsuarioRepository usuarioRepository, 
        ITutoriaRepository tutoriaRepository,
        ITutoriaEstudianteRepository tutoriaEstudianteRepository,
        IHorarioRepository horarioRepository,
        ICorreoService correoService,
        IEmailTemplateService emailTemplateService,
        IExcelService excelService)
    {
        _notificacionRepository = notificacionRepository;
        _usuarioRepository = usuarioRepository;
        _tutoriaRepository = tutoriaRepository;
        _tutoriaEstudianteRepository = tutoriaEstudianteRepository;
        _horarioRepository = horarioRepository;
        _correoService = correoService;
        _emailTemplateService = emailTemplateService;
        _excelService = excelService;
    }

    public async Task<NotificacionResponseDto> CreateNotificacionAsync(NotificacionCreateDto notificacionDto)
    {
        // Verificar que el usuario existe
        var usuario = await _usuarioRepository.GetByIdAsync(notificacionDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Crear notificación
        var notificacion = new Notificacion
        {
            fecha = DateTime.Now,
            asunto = notificacionDto.Asunto,
            descripcion = notificacionDto.Descripcion,
            Usuario_idUsuario = notificacionDto.UsuarioId
        };

        var notificacionCreada = await _notificacionRepository.CreateAsync(notificacion);

        return new NotificacionResponseDto
        {
            IdNotificacion = notificacionCreada.idNotificacion,
            Fecha = notificacionCreada.fecha,
            Asunto = notificacionCreada.asunto,
            Descripcion = notificacionCreada.descripcion,
            UsuarioNombre = usuario.nombres,
            UsuarioApellidos = usuario.apellidos,
            UsuarioCorreo = usuario.correo
        };
    }

    public async Task<NotificacionResponseDto> GetNotificacionByIdAsync(int id)
    {
        var notificacion = await _notificacionRepository.GetByIdAsync(id);
        if (notificacion == null)
        {
            throw new NotFoundException("Notificación no encontrada");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(notificacion.Usuario_idUsuario);

        return new NotificacionResponseDto
        {
            IdNotificacion = notificacion.idNotificacion,
            Fecha = notificacion.fecha,
            Asunto = notificacion.asunto,
            Descripcion = notificacion.descripcion,
            UsuarioNombre = usuario?.nombres ?? "N/A",
            UsuarioApellidos = usuario?.apellidos ?? "N/A",
            UsuarioCorreo = usuario?.correo ?? "N/A"
        };
    }

    public async Task<List<NotificacionResponseDto>> GetAllNotificacionesAsync()
    {
        var notificaciones = await _notificacionRepository.GetAllAsync();
        var notificacionesDto = new List<NotificacionResponseDto>();

        foreach (var notificacion in notificaciones)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(notificacion.Usuario_idUsuario);
            
            notificacionesDto.Add(new NotificacionResponseDto
            {
                IdNotificacion = notificacion.idNotificacion,
                Fecha = notificacion.fecha,
                Asunto = notificacion.asunto,
                Descripcion = notificacion.descripcion,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A",
                UsuarioCorreo = usuario?.correo ?? "N/A"
            });
        }

        return notificacionesDto;
    }

    public async Task<NotificacionResponseDto> UpdateNotificacionAsync(int id, NotificacionCreateDto notificacionDto)
    {
        var notificacion = await _notificacionRepository.GetByIdAsync(id);
        if (notificacion == null)
        {
            throw new NotFoundException("Notificación no encontrada");
        }

        // Verificar que el usuario existe
        var usuario = await _usuarioRepository.GetByIdAsync(notificacionDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Actualizar datos
        notificacion.asunto = notificacionDto.Asunto;
        notificacion.descripcion = notificacionDto.Descripcion;
        notificacion.Usuario_idUsuario = notificacionDto.UsuarioId;

        await _notificacionRepository.UpdateAsync(notificacion);

        return new NotificacionResponseDto
        {
            IdNotificacion = notificacion.idNotificacion,
            Fecha = notificacion.fecha,
            Asunto = notificacion.asunto,
            Descripcion = notificacion.descripcion,
            UsuarioNombre = usuario.nombres,
            UsuarioApellidos = usuario.apellidos,
            UsuarioCorreo = usuario.correo
        };
    }

    public async Task<bool> DeleteNotificacionAsync(int id)
    {
        var notificacion = await _notificacionRepository.GetByIdAsync(id);
        if (notificacion == null)
        {
            throw new NotFoundException("Notificación no encontrada");
        }

        return await _notificacionRepository.DeleteAsync(id);
    }

    public async Task<List<NotificacionResponseDto>> GetNotificacionesByUsuarioAsync(int usuarioId)
    {
        var notificaciones = await _notificacionRepository.GetByUsuarioAsync(usuarioId);
        var notificacionesDto = new List<NotificacionResponseDto>();

        foreach (var notificacion in notificaciones)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(notificacion.Usuario_idUsuario);
            
            notificacionesDto.Add(new NotificacionResponseDto
            {
                IdNotificacion = notificacion.idNotificacion,
                Fecha = notificacion.fecha,
                Asunto = notificacion.asunto,
                Descripcion = notificacion.descripcion,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A",
                UsuarioCorreo = usuario?.correo ?? "N/A"
            });
        }

        return notificacionesDto;
    }

    public async Task<bool> EnviarNotificacionTutoriaAsync(int tutoriaId, string tipoNotificacion)
    {
        try
        {
            var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
            if (tutoria == null)
            {
                throw new NotFoundException("Tutoría no encontrada");
            }

            var usuario = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
            if (usuario == null)
            {
                throw new NotFoundException("Usuario no encontrado");
            }

            string asunto = "";
            string descripcion = "";

            switch (tipoNotificacion.ToLower())
            {
                case "asignacion":
                    asunto = "Nueva Tutoría Asignada";
                    descripcion = $"Se le ha asignado una nueva tutoría de {tutoria.idioma} nivel {tutoria.nivel} para el tema: {tutoria.tema}";
                    break;
                case "confirmacion":
                    asunto = "Confirmación de Tutoría";
                    descripcion = $"Su tutoría de {tutoria.idioma} nivel {tutoria.nivel} ha sido confirmada para el {tutoria.fechaHora:dd/MM/yyyy}";
                    break;
                case "cancelacion":
                    asunto = "Tutoría Cancelada";
                    descripcion = $"Su tutoría de {tutoria.idioma} nivel {tutoria.nivel} programada para el {tutoria.fechaHora:dd/MM/yyyy} ha sido cancelada";
                    break;
                default:
                    asunto = "Notificación de Tutoría";
                    descripcion = $"Información sobre su tutoría de {tutoria.idioma} nivel {tutoria.nivel}";
                    break;
            }

            var notificacion = new Notificacion
            {
                fecha = DateTime.Now,
                asunto = asunto,
                descripcion = descripcion,
                Usuario_idUsuario = usuario.idUsuario
            };

            await _notificacionRepository.CreateAsync(notificacion);

            // Aquí se implementaría el envío del correo electrónico
            // await EnviarCorreoAsync(usuario.correo, asunto, descripcion);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> EnviarRecordatorioAsync(int tutoriaId)
    {
        try
        {
            var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
            if (tutoria == null)
            {
                throw new NotFoundException("Tutoría no encontrada");
            }

            var usuario = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
            if (usuario == null)
            {
                throw new NotFoundException("Usuario no encontrado");
            }

            var asunto = "Recordatorio de Tutoría";
            var descripcion = $"Recordatorio: Su tutoría de {tutoria.idioma} nivel {tutoria.nivel} está programada para el {tutoria.fechaHora:dd/MM/yyyy} a las {tutoria.fechaHora:HH:mm}";

            var notificacion = new Notificacion
            {
                fecha = DateTime.Now,
                asunto = asunto,
                descripcion = descripcion,
                Usuario_idUsuario = usuario.idUsuario
            };

            await _notificacionRepository.CreateAsync(notificacion);

            // Aquí se implementaría el envío del correo electrónico
            // await EnviarCorreoAsync(usuario.correo, asunto, descripcion);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> EnviarNotificacionCambioAsync(int tutoriaId, string motivo)
    {
        try
        {
            var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
            if (tutoria == null)
            {
                throw new NotFoundException("Tutoría no encontrada");
            }

            var usuario = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
            if (usuario == null)
            {
                throw new NotFoundException("Usuario no encontrado");
            }

            var asunto = "Cambio en Tutoría";
            var descripcion = $"Se ha realizado un cambio en su tutoría de {tutoria.idioma} nivel {tutoria.nivel}. Motivo: {motivo}";

            var notificacion = new Notificacion
            {
                fecha = DateTime.Now,
                asunto = asunto,
                descripcion = descripcion,
                Usuario_idUsuario = usuario.idUsuario
            };

            await _notificacionRepository.CreateAsync(notificacion);

            // Aquí se implementaría el envío del correo electrónico
            // await EnviarCorreoAsync(usuario.correo, asunto, descripcion);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Nuevos métodos para envío automático de emails
    public async Task<bool> EnviarNotificacionTutoriaPorEmailAsync(int tutoriaId, string tipoNotificacion)
    {
        try
        {
            var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
            if (tutoria == null) return false;

            var tutor = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
            if (tutor == null) return false;

            // Obtener estudiantes asignados a la tutoría
            var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);
            var estudiantes = new List<UsuarioResponseDto>();

            foreach (var te in tutoriaEstudiantes)
            {
                var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
                if (estudiante != null && estudiante.Rol != null && estudiante.Rol.tipoRol.ToLower() == "estudiante")
                {
                    estudiantes.Add(new UsuarioResponseDto
                    {
                        IdUsuario = estudiante.idUsuario,
                        Nombres = estudiante.nombres,
                        Apellidos = estudiante.apellidos,
                        Correo = estudiante.correo
                    });
                }
            }

            // Si no hay estudiantes, al menos enviar al tutor
            if (estudiantes.Count == 0)
            {
                estudiantes.Add(new UsuarioResponseDto
                {
                    IdUsuario = tutor.idUsuario,
                    Nombres = tutor.nombres,
                    Apellidos = tutor.apellidos,
                    Correo = tutor.correo
                });
            }

            var tutoriaDto = new TutoriaResponseDto
            {
                IdTutoria = tutoria.idTutoria,
                Idioma = tutoria.idioma,
                Nivel = tutoria.nivel,
                Tema = tutoria.tema,
                Modalidad = tutoria.modalidad,
                Estado = tutoria.estado,
                FechaTutoria = tutoria.fechaHora
            };

            foreach (var estudiante in estudiantes)
            {
                string htmlContent = "";
                string asunto = "";
                string descripcion = "";

                switch (tipoNotificacion.ToLower())
                {
                    case "asignacion":
                        htmlContent = _emailTemplateService.GenerarTemplateNotificacionTutoria(tutoriaDto, estudiante);
                        asunto = $"Nueva Tutoría Asignada - {tutoria.idioma} {tutoria.nivel}";
                        descripcion = $"Se le ha asignado una nueva tutoría de {tutoria.idioma} nivel {tutoria.nivel} para el tema: {tutoria.tema}";
                        break;
                    case "recordatorio":
                        htmlContent = _emailTemplateService.GenerarTemplateRecordatorio(tutoriaDto, estudiante);
                        asunto = $"Recordatorio de Tutoría - {tutoria.fechaHora:dd/MM/yyyy}";
                        descripcion = $"Recordatorio: Su tutoría de {tutoria.idioma} nivel {tutoria.nivel} está programada para el {tutoria.fechaHora:dd/MM/yyyy} a las {tutoria.fechaHora:HH:mm}";
                        break;
                }

                if (!string.IsNullOrEmpty(htmlContent))
                {
                    _correoService.EnviarCorreo(estudiante.Correo, asunto, htmlContent);

                    // Guardar notificación en la base de datos
                    var notificacion = new Notificacion
                    {
                        fecha = DateTime.Now,
                        asunto = asunto,
                        descripcion = descripcion,
                        Usuario_idUsuario = estudiante.IdUsuario
                    };
                    await _notificacionRepository.CreateAsync(notificacion);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando notificación por email: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EnviarRecordatorioPorEmailAsync(int tutoriaId)
    {
        try
        {
            var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
            if (tutoria == null) return false;

            // Obtener estudiantes asignados a la tutoría
            var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);
            
            if (tutoriaEstudiantes == null || tutoriaEstudiantes.Count == 0)
            {
                Console.WriteLine($"No hay estudiantes asignados a la tutoría {tutoriaId}");
                return false;
            }

            var tutoriaDto = new TutoriaResponseDto
            {
                IdTutoria = tutoria.idTutoria,
                Idioma = tutoria.idioma,
                Nivel = tutoria.nivel,
                Tema = tutoria.tema,
                Modalidad = tutoria.modalidad,
                Estado = tutoria.estado,
                FechaTutoria = tutoria.fechaHora
            };

            string asunto = $"Recordatorio de Tutoría - {tutoria.fechaHora:dd/MM/yyyy HH:mm}";
            string descripcion = $"Recordatorio: Su tutoría de {tutoria.idioma} nivel {tutoria.nivel} está programada para el {tutoria.fechaHora:dd/MM/yyyy} a las {tutoria.fechaHora:HH:mm}";

            foreach (var te in tutoriaEstudiantes)
            {
                var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
                if (estudiante != null && estudiante.Rol != null && estudiante.Rol.tipoRol.ToLower() == "estudiante")
                {
                    var estudianteDto = new UsuarioResponseDto
                    {
                        IdUsuario = estudiante.idUsuario,
                        Nombres = estudiante.nombres,
                        Apellidos = estudiante.apellidos,
                        Correo = estudiante.correo
                    };

                    string htmlContent = _emailTemplateService.GenerarTemplateRecordatorio(tutoriaDto, estudianteDto);
                    
                    _correoService.EnviarCorreo(estudianteDto.Correo, asunto, htmlContent);

                    // Guardar notificación en la base de datos
                    var notificacion = new Notificacion
                    {
                        fecha = DateTime.Now,
                        asunto = asunto,
                        descripcion = descripcion,
                        Usuario_idUsuario = estudiante.idUsuario
                    };
                    await _notificacionRepository.CreateAsync(notificacion);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando recordatorio por email: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EnviarNotificacionCambioPorEmailAsync(int tutoriaId, string motivo, TutoriaResponseDto tutoriaAnterior = null)
    {
        try
        {
            var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
            if (tutoria == null) return false;

            var horarioActual = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);
            var tutorActual = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);

            // Obtener estudiantes asignados a la tutoría
            var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);
            
            if (tutoriaEstudiantes == null || tutoriaEstudiantes.Count == 0)
            {
                Console.WriteLine($"No hay estudiantes asignados a la tutoría {tutoriaId}");
                return false;
            }

            var tutoriaDto = new TutoriaResponseDto
            {
                IdTutoria = tutoria.idTutoria,
                Idioma = tutoria.idioma,
                Nivel = tutoria.nivel,
                Tema = tutoria.tema,
                Modalidad = tutoria.modalidad,
                Estado = tutoria.estado,
                FechaTutoria = tutoria.fechaHora,
                UsuarioNombre = tutorActual?.nombres ?? "",
                UsuarioApellidos = tutorActual?.apellidos ?? "",
                HorarioEspacio = horarioActual?.espacio ?? ""
            };

            // Detectar cambios comparando datos anteriores con actuales
            var cambios = new CambioTutoriaDto();
            
            if (tutoriaAnterior != null)
            {
                // Comparar fecha
                if (tutoriaAnterior.FechaTutoria.Date != tutoria.fechaHora.Date)
                {
                    cambios.CambioFecha = $"Fecha cambió de {tutoriaAnterior.FechaTutoria:dd/MM/yyyy} a {tutoria.fechaHora:dd/MM/yyyy}";
                }

                // Comparar hora
                if (tutoriaAnterior.FechaTutoria.TimeOfDay != tutoria.fechaHora.TimeOfDay)
                {
                    cambios.CambioHora = $"Hora cambió de {tutoriaAnterior.FechaTutoria:HH:mm} a {tutoria.fechaHora:HH:mm}";
                }

                // Comparar tutor
                var tutorAnteriorNombre = $"{tutoriaAnterior.UsuarioNombre} {tutoriaAnterior.UsuarioApellidos}".Trim();
                var tutorActualNombre = $"{tutorActual?.nombres} {tutorActual?.apellidos}".Trim();
                if (tutorAnteriorNombre != tutorActualNombre)
                {
                    cambios.CambioTutor = $"Profesor/Tutor cambió de {tutorAnteriorNombre} a {tutorActualNombre}";
                }

                // Comparar aula/espacio
                if (tutoriaAnterior.HorarioEspacio != horarioActual?.espacio)
                {
                    cambios.CambioAula = $"Aula cambió de {tutoriaAnterior.HorarioEspacio} a {horarioActual?.espacio}";
                }

                // Comparar tema
                if (tutoriaAnterior.Tema != tutoria.tema)
                {
                    cambios.CambioTema = $"Tema cambió de '{tutoriaAnterior.Tema}' a '{tutoria.tema}'";
                }

                // Comparar idioma
                if (tutoriaAnterior.Idioma != tutoria.idioma)
                {
                    cambios.CambioIdioma = $"Idioma cambió de {tutoriaAnterior.Idioma} a {tutoria.idioma}";
                }

                // Comparar nivel
                if (tutoriaAnterior.Nivel != tutoria.nivel)
                {
                    cambios.CambioNivel = $"Nivel cambió de {tutoriaAnterior.Nivel} a {tutoria.nivel}";
                }

                // Comparar modalidad
                if (tutoriaAnterior.Modalidad != tutoria.modalidad)
                {
                    cambios.CambioModalidad = $"Modalidad cambió de {tutoriaAnterior.Modalidad} a {tutoria.modalidad}";
                }
            }

            string asunto = $"Cambio en Tutoría - {tutoria.idioma} {tutoria.nivel}";
            var cambiosTexto = cambios.ObtenerListaCambios();
            string descripcion = cambiosTexto.Count > 0 
                ? $"Se ha realizado un cambio en su tutoría de {tutoria.idioma} nivel {tutoria.nivel}. Cambios: {string.Join(", ", cambiosTexto)}. Motivo: {motivo}"
                : $"Se ha realizado un cambio en su tutoría de {tutoria.idioma} nivel {tutoria.nivel}. Motivo: {motivo}";

            foreach (var te in tutoriaEstudiantes)
            {
                var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
                if (estudiante != null && estudiante.Rol != null && estudiante.Rol.tipoRol.ToLower() == "estudiante")
                {
                    var estudianteDto = new UsuarioResponseDto
                    {
                        IdUsuario = estudiante.idUsuario,
                        Nombres = estudiante.nombres,
                        Apellidos = estudiante.apellidos,
                        Correo = estudiante.correo
                    };

                    string htmlContent = _emailTemplateService.GenerarTemplateCambio(tutoriaDto, estudianteDto, motivo, cambios);
                    
                    _correoService.EnviarCorreo(estudianteDto.Correo, asunto, htmlContent);

                    // Guardar notificación en la base de datos
                    var notificacion = new Notificacion
                    {
                        fecha = DateTime.Now,
                        asunto = asunto,
                        descripcion = descripcion,
                        Usuario_idUsuario = estudiante.idUsuario
                    };
                    await _notificacionRepository.CreateAsync(notificacion);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando notificación de cambio por email: {ex.Message}");
            return false;
        }
    }

    public async Task<(byte[] archivoBytes, string nombreArchivo)> GenerarReporteExcelParaDescargaAsync(int tutoriaId, int docenteId)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        var docente = await _usuarioRepository.GetByIdAsync(docenteId);
        if (docente == null)
        {
            throw new NotFoundException("Docente no encontrado");
        }

        var horario = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);

        var tutoriaDto = new TutoriaResponseDto
        {
            IdTutoria = tutoria.idTutoria,
            Idioma = tutoria.idioma,
            Nivel = tutoria.nivel,
            Tema = tutoria.tema,
            Modalidad = tutoria.modalidad,
            Estado = tutoria.estado,
            FechaTutoria = tutoria.fechaHora,
            HorarioEspacio = horario?.espacio ?? "N/A",
            HorarioHoraInicio = horario?.horaInicio ?? DateTime.MinValue,
            HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue
        };

        var docenteDto = new UsuarioResponseDto
        {
            IdUsuario = docente.idUsuario,
            Nombres = docente.nombres,
            Apellidos = docente.apellidos,
            Correo = docente.correo
        };

        // Obtener estudiantes reales de la tutoría
        var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);
        var estudiantes = new List<UsuarioResponseDto>();

        foreach (var te in tutoriaEstudiantes)
        {
            // Usar el Usuario ya cargado en TutoriaEstudiante (incluido en GetByTutoriaAsync)
            if (te.Usuario != null && te.Usuario.Rol != null && te.Usuario.Rol.tipoRol.ToLower() == "estudiante")
            {
                estudiantes.Add(new UsuarioResponseDto
                {
                    IdUsuario = te.Usuario.idUsuario,
                    Nombres = te.Usuario.nombres,
                    Apellidos = te.Usuario.apellidos,
                    Correo = te.Usuario.correo
                });
            }
        }

        // Generar reporte Excel con estudiantes reales
        var reporteBytes = _excelService.GenerarReporteTutoria(tutoriaDto, docenteDto, estudiantes);
        var nombreArchivo = _excelService.GenerarNombreArchivo(tutoriaId, $"{docente.nombres}_{docente.apellidos}");

        return (reporteBytes, nombreArchivo);
    }

    public async Task<bool> EnviarNotificacionEstudianteTutoriaAsync(TutoriaResponseDto tutoriaDto, UsuarioResponseDto estudianteDto, string tipoNotificacion)
    {
        try
        {
            string htmlContent = "";
            string asunto = "";
            string descripcion = "";

            switch (tipoNotificacion.ToLower())
            {
                case "asignacion":
                    htmlContent = _emailTemplateService.GenerarTemplateNotificacionTutoria(tutoriaDto, estudianteDto);
                    asunto = $"Nueva Tutoría Asignada - {tutoriaDto.Idioma} {tutoriaDto.Nivel}";
                    descripcion = $"Se le ha asignado una nueva tutoría de {tutoriaDto.Idioma} nivel {tutoriaDto.Nivel} para el tema: {tutoriaDto.Tema}";
                    break;
                case "recordatorio":
                    htmlContent = _emailTemplateService.GenerarTemplateRecordatorio(tutoriaDto, estudianteDto);
                    asunto = $"Recordatorio de Tutoría - {tutoriaDto.FechaTutoria:dd/MM/yyyy}";
                    descripcion = $"Recordatorio: Su tutoría de {tutoriaDto.Idioma} nivel {tutoriaDto.Nivel} está programada para el {tutoriaDto.FechaTutoria:dd/MM/yyyy} a las {tutoriaDto.FechaTutoria:HH:mm}";
                    break;
                case "cambio":
                    htmlContent = _emailTemplateService.GenerarTemplateCambio(tutoriaDto, estudianteDto, "", new CambioTutoriaDto());
                    asunto = $"Cambio en Tutoría - {tutoriaDto.Idioma} {tutoriaDto.Nivel}";
                    descripcion = $"Se ha realizado un cambio en su tutoría de {tutoriaDto.Idioma} nivel {tutoriaDto.Nivel}";
                    break;
                default:
                    htmlContent = _emailTemplateService.GenerarTemplateNotificacionTutoria(tutoriaDto, estudianteDto);
                    asunto = $"Notificación de Tutoría - {tutoriaDto.Idioma} {tutoriaDto.Nivel}";
                    descripcion = $"Información sobre su tutoría de {tutoriaDto.Idioma} nivel {tutoriaDto.Nivel}";
                    break;
            }

            if (!string.IsNullOrEmpty(htmlContent))
            {
                _correoService.EnviarCorreo(estudianteDto.Correo, asunto, htmlContent);

                // Guardar notificación en la base de datos
                var notificacion = new Notificacion
                {
                    fecha = DateTime.Now,
                    asunto = asunto,
                    descripcion = descripcion,
                    Usuario_idUsuario = estudianteDto.IdUsuario
                };
                await _notificacionRepository.CreateAsync(notificacion);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando notificación a estudiante: {ex.Message}");
            return false;
        }
    }
}
