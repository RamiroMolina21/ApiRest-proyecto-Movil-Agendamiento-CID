using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Services;

public class TutoriaService : ITutoriaService
{
    private readonly ITutoriaRepository _tutoriaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IHorarioRepository _horarioRepository;
    private readonly INotificacionService _notificacionService;
    private readonly IRolRepository _rolRepository;
    private readonly ITutoriaEstudianteRepository _tutoriaEstudianteRepository;

    public TutoriaService(ITutoriaRepository tutoriaRepository, IUsuarioRepository usuarioRepository, IHorarioRepository horarioRepository, INotificacionService notificacionService, IRolRepository rolRepository, ITutoriaEstudianteRepository tutoriaEstudianteRepository)
    {
        _tutoriaRepository = tutoriaRepository;
        _usuarioRepository = usuarioRepository;
        _horarioRepository = horarioRepository;
        _notificacionService = notificacionService;
        _rolRepository = rolRepository;
        _tutoriaEstudianteRepository = tutoriaEstudianteRepository;
    }

    public async Task<TutoriaResponseDto> CreateTutoriaAsync(TutoriaCreateDto tutoriaDto)
    {
        // Buscar tutor por ID
        var tutor = await _usuarioRepository.GetByIdAsync(tutoriaDto.UsuarioId);
        if (tutor == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Verificar que el horario existe
        var horario = await _horarioRepository.GetByIdAsync(tutoriaDto.HorarioId);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        // Cambiar estado del horario de "Libre" a "Ocupado"
        if (horario.estado.ToLower() == "libre" || horario.estado.ToLower() == "disponible")
        {
            horario.estado = "Ocupado";
            await _horarioRepository.UpdateAsync(horario);
        }

        // Crear tutoría
        var tutoria = new Tutoria
        {
            idioma = tutoriaDto.Idioma,
            nivel = tutoriaDto.Nivel,
            tema = tutoriaDto.Tema,
            modalidad = tutoriaDto.Modalidad,
            estado = tutoriaDto.Estado,
            fechaHora = tutoriaDto.FechaTutoria,
            Usuario_idUsuario = tutor.idUsuario,
            Horario_idHorario = tutoriaDto.HorarioId
        };

        var tutoriaCreada = await _tutoriaRepository.CreateAsync(tutoria);

        // Enviar notificación automática por email al tutor
        try
        {
            await _notificacionService.EnviarNotificacionTutoriaPorEmailAsync(tutoriaCreada.idTutoria, "asignacion");
        }
        catch (Exception ex)
        {
            // Log del error pero no fallar la creación de la tutoría
            Console.WriteLine($"Error enviando notificación automática: {ex.Message}");
        }

        return new TutoriaResponseDto
        {
            IdTutoria = tutoriaCreada.idTutoria,
            Idioma = tutoriaCreada.idioma,
            Nivel = tutoriaCreada.nivel,
            Tema = tutoriaCreada.tema,
            Modalidad = tutoriaCreada.modalidad,
            Estado = tutoriaCreada.estado,
            FechaTutoria = tutoriaCreada.fechaHora,
            UsuarioNombre = tutor.nombres,
            UsuarioApellidos = tutor.apellidos,
            UsuarioCorreo = tutor.correo,
            HorarioEspacio = horario.espacio,
            HorarioHoraInicio = horario.horaInicio,
            HorarioHoraFin = horario.horaFin
        };
    }

    public async Task<TutoriaResponseDto> GetTutoriaByIdAsync(int id)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(id);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        var tutor = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
        var horario = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);

        // Obtener estudiantes asignados a la tutoría
        var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(id);
        var estudiantes = new List<UsuarioResponseDto>();

        foreach (var te in tutoriaEstudiantes)
        {
            var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
            if (estudiante != null)
            {
                estudiantes.Add(new UsuarioResponseDto
                {
                    IdUsuario = estudiante.idUsuario,
                    Nombres = estudiante.nombres,
                    Apellidos = estudiante.apellidos,
                    Correo = estudiante.correo,
                    FechaRegistro = estudiante.fechaRegistro,
                    TipoRol = estudiante.Rol?.tipoRol ?? "Sin rol"
                });
            }
        }

        // Información del profesor/tutor
        var profesorDto = tutor != null ? new UsuarioResponseDto
        {
            IdUsuario = tutor.idUsuario,
            Nombres = tutor.nombres,
            Apellidos = tutor.apellidos,
            Correo = tutor.correo,
            FechaRegistro = tutor.fechaRegistro,
            TipoRol = tutor.Rol?.tipoRol ?? "Sin rol"
        } : null;

        return new TutoriaResponseDto
        {
            IdTutoria = tutoria.idTutoria,
            Idioma = tutoria.idioma,
            Nivel = tutoria.nivel,
            Tema = tutoria.tema,
            Modalidad = tutoria.modalidad,
            Estado = tutoria.estado,
            FechaTutoria = tutoria.fechaHora,
            UsuarioNombre = tutor?.nombres ?? "N/A",
            UsuarioApellidos = tutor?.apellidos ?? "N/A",
            UsuarioCorreo = tutor?.correo ?? "N/A",
            HorarioEspacio = horario?.espacio ?? "N/A",
            HorarioHoraInicio = horario?.horaInicio ?? DateTime.MinValue,
            HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue,
            Profesor = profesorDto,
            Estudiantes = estudiantes
        };
    }

    public async Task<List<TutoriaResponseDto>> GetAllTutoriasAsync()
    {
        var tutorias = await _tutoriaRepository.GetAllAsync();
        var tutoriasDto = new List<TutoriaResponseDto>();

        foreach (var tutoria in tutorias)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
            var horario = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);
            
            tutoriasDto.Add(new TutoriaResponseDto
            {
                IdTutoria = tutoria.idTutoria,
                Idioma = tutoria.idioma,
                Nivel = tutoria.nivel,
                Tema = tutoria.tema,
                Modalidad = tutoria.modalidad,
                Estado = tutoria.estado,
                FechaTutoria = tutoria.fechaHora,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A",
                HorarioEspacio = horario?.espacio ?? "N/A",
                HorarioHoraInicio = horario?.horaInicio ?? DateTime.MinValue,
                HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue
            });
        }

        return tutoriasDto;
    }

    public async Task<TutoriaResponseDto> UpdateTutoriaAsync(int id, TutoriaCreateDto tutoriaDto)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(id);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        // Buscar tutor por ID
        var tutor = await _usuarioRepository.GetByIdAsync(tutoriaDto.UsuarioId);
        if (tutor == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Verificar que el horario existe
        var horario = await _horarioRepository.GetByIdAsync(tutoriaDto.HorarioId);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        // Guardar datos anteriores para detectar cambios
        var horarioAnterior = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);
        var tutorAnterior = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
        var tutoriaAnterior = new TutoriaResponseDto
        {
            IdTutoria = tutoria.idTutoria,
            Idioma = tutoria.idioma,
            Nivel = tutoria.nivel,
            Tema = tutoria.tema,
            Modalidad = tutoria.modalidad,
            Estado = tutoria.estado,
            FechaTutoria = tutoria.fechaHora,
            UsuarioNombre = tutorAnterior?.nombres ?? "",
            UsuarioApellidos = tutorAnterior?.apellidos ?? "",
            HorarioEspacio = horarioAnterior?.espacio ?? ""
        };

        // Actualizar datos
        tutoria.idioma = tutoriaDto.Idioma;
        tutoria.nivel = tutoriaDto.Nivel;
        tutoria.tema = tutoriaDto.Tema;
        tutoria.modalidad = tutoriaDto.Modalidad;
        tutoria.estado = tutoriaDto.Estado;
        tutoria.fechaHora = tutoriaDto.FechaTutoria;
        tutoria.Usuario_idUsuario = tutor.idUsuario;
        tutoria.Horario_idHorario = tutoriaDto.HorarioId;

        await _tutoriaRepository.UpdateAsync(tutoria);

        var tutoriaActualizada = new TutoriaResponseDto
        {
            IdTutoria = tutoria.idTutoria,
            Idioma = tutoria.idioma,
            Nivel = tutoria.nivel,
            Tema = tutoria.tema,
            Modalidad = tutoria.modalidad,
            Estado = tutoria.estado,
            FechaTutoria = tutoria.fechaHora,
            UsuarioNombre = tutor.nombres,
            UsuarioApellidos = tutor.apellidos,
            UsuarioCorreo = tutor.correo,
            HorarioEspacio = horario.espacio,
            HorarioHoraInicio = horario.horaInicio,
            HorarioHoraFin = horario.horaFin
        };

        // Enviar notificación de cambio a los estudiantes si hay cambios
        try
        {
            await _notificacionService.EnviarNotificacionCambioPorEmailAsync(tutoria.idTutoria, "Cambio realizado en la tutoría", tutoriaAnterior);
        }
        catch (Exception ex)
        {
            // Log del error pero no fallar la actualización
            Console.WriteLine($"Error enviando notificación de cambio: {ex.Message}");
        }

        return tutoriaActualizada;
    }

    public async Task<bool> DeleteTutoriaAsync(int id)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(id);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        return await _tutoriaRepository.DeleteAsync(id);
    }

    public async Task<bool> AgregarEstudiantesATutoriaAsync(int tutoriaId, AgregarEstudiantesTutoriaDto estudiantesDto)
    {
        // Verificar que la tutoría existe
        var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        // Buscar rol estudiante
        var rolEstudiante = await _rolRepository.GetByTipoAsync("Estudiante");
        if (rolEstudiante == null)
        {
            throw new NotFoundException("El rol 'Estudiante' no existe en el sistema");
        }

        var estudiantesAgregados = new List<Usuario>();

        // Procesar cada estudiante
        foreach (var estudianteDto in estudiantesDto.Estudiantes)
        {
            // Validar formato de correo
            EmailValidator.ValidateEmail(estudianteDto.Correo, "Correo del estudiante");

            // Buscar estudiante por nombre, apellidos y correo
            var estudiante = await _usuarioRepository.GetByNombreApellidosCorreoAsync(
                estudianteDto.Nombre,
                estudianteDto.Apellidos,
                estudianteDto.Correo);

            // Si no existe, crear nuevo estudiante
            if (estudiante == null)
            {
                var estudiantePorCorreo = await _usuarioRepository.GetByEmailAsync(estudianteDto.Correo);
                if (estudiantePorCorreo != null)
                {
                    throw new ValidationException($"Ya existe un usuario con el correo {estudianteDto.Correo} pero con datos diferentes");
                }

                estudiante = new Usuario
                {
                    nombres = estudianteDto.Nombre,
                    apellidos = estudianteDto.Apellidos,
                    correo = estudianteDto.Correo,
                    contrasenaHash = BCrypt.Net.BCrypt.HashPassword("Temporal123!"), // Contraseña temporal
                    fechaRegistro = DateTime.Now,
                    Rol_idRol = rolEstudiante.idRol
                };

                estudiante = await _usuarioRepository.CreateAsync(estudiante);
            }
            else
            {
                // Verificar que el usuario tenga rol de estudiante
                if (estudiante.Rol == null || estudiante.Rol.tipoRol.ToLower() != "estudiante")
                {
                    // Si no es estudiante, actualizar el rol
                    estudiante.Rol_idRol = rolEstudiante.idRol;
                    await _usuarioRepository.UpdateAsync(estudiante);
                }
            }

            // Verificar que el estudiante no esté ya asignado a esta tutoría
            var existeAsignacion = await _tutoriaEstudianteRepository.ExistsAsync(tutoriaId, estudiante.idUsuario);
            if (!existeAsignacion)
            {
                // Crear relación TutoriaEstudiante
                var tutoriaEstudiante = new TutoriaEstudiante
                {
                    Tutoria_idTutoria = tutoriaId,
                    Usuario_idUsuario = estudiante.idUsuario
                };

                await _tutoriaEstudianteRepository.CreateAsync(tutoriaEstudiante);
                estudiantesAgregados.Add(estudiante);

                // Enviar notificación por email al estudiante
                try
                {
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

                    var estudianteResponseDto = new UsuarioResponseDto
                    {
                        IdUsuario = estudiante.idUsuario,
                        Nombres = estudiante.nombres,
                        Apellidos = estudiante.apellidos,
                        Correo = estudiante.correo
                    };

                    // Enviar notificación de asignación
                    await _notificacionService.EnviarNotificacionEstudianteTutoriaAsync(tutoriaDto, estudianteResponseDto, "asignacion");
                }
                catch (Exception ex)
                {
                    // Log del error pero continuar
                    Console.WriteLine($"Error enviando notificación a estudiante {estudiante.correo}: {ex.Message}");
                }
            }
        }

        return estudiantesAgregados.Count > 0;
    }

    public async Task<List<TutoriaResponseDto>> GetCalendarioPorDocenteAsync(string nombre, string apellidos, string correo)
    {
        // Buscar docente por nombre, apellidos y correo
        var docente = await _usuarioRepository.GetByNombreApellidosCorreoAsync(nombre, apellidos, correo);
        if (docente == null)
        {
            // Si no se encuentra con todos los datos, intentar por correo
            docente = await _usuarioRepository.GetByEmailAsync(correo);
            if (docente == null)
            {
                throw new NotFoundException("Docente no encontrado");
            }
        }

        // Verificar que tenga rol de docente/tutor
        if (docente.Rol == null || (docente.Rol.tipoRol.ToLower() != "docente" && docente.Rol.tipoRol.ToLower() != "tutor"))
        {
            throw new ValidationException("El usuario especificado no es un docente/tutor");
        }

        // Obtener tutorías del docente
        var tutorias = await _tutoriaRepository.GetByUsuarioAsync(docente.idUsuario);
        var tutoriasDto = new List<TutoriaResponseDto>();

        foreach (var tutoria in tutorias)
        {
            var horario = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);

            // Obtener estudiantes asignados
            var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoria.idTutoria);
            var estudiantes = new List<UsuarioResponseDto>();

            foreach (var te in tutoriaEstudiantes)
            {
                var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
                if (estudiante != null)
                {
                    estudiantes.Add(new UsuarioResponseDto
                    {
                        IdUsuario = estudiante.idUsuario,
                        Nombres = estudiante.nombres,
                        Apellidos = estudiante.apellidos,
                        Correo = estudiante.correo,
                        FechaRegistro = estudiante.fechaRegistro,
                        TipoRol = estudiante.Rol?.tipoRol ?? "Sin rol"
                    });
                }
            }

            // Información del profesor/tutor
            var profesorDto = new UsuarioResponseDto
            {
                IdUsuario = docente.idUsuario,
                Nombres = docente.nombres,
                Apellidos = docente.apellidos,
                Correo = docente.correo,
                FechaRegistro = docente.fechaRegistro,
                TipoRol = docente.Rol?.tipoRol ?? "Sin rol"
            };

            tutoriasDto.Add(new TutoriaResponseDto
            {
                IdTutoria = tutoria.idTutoria,
                Idioma = tutoria.idioma,
                Nivel = tutoria.nivel,
                Tema = tutoria.tema,
                Modalidad = tutoria.modalidad,
                Estado = tutoria.estado,
                FechaTutoria = tutoria.fechaHora,
                UsuarioNombre = docente.nombres,
                UsuarioApellidos = docente.apellidos,
                UsuarioCorreo = docente.correo,
                HorarioEspacio = horario?.espacio ?? "N/A",
                HorarioHoraInicio = horario?.horaInicio ?? DateTime.MinValue,
                HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue,
                Profesor = profesorDto,
                Estudiantes = estudiantes
            });
        }

        return tutoriasDto;
    }

    public async Task<List<TutoriaResponseDto>> GetCalendarioPorEstudianteAsync(string nombre, string apellidos, string correo)
    {
        // Buscar estudiante por nombre, apellidos y correo
        var estudiante = await _usuarioRepository.GetByNombreApellidosCorreoAsync(nombre, apellidos, correo);
        if (estudiante == null)
        {
            // Si no se encuentra con todos los datos, intentar por correo
            estudiante = await _usuarioRepository.GetByEmailAsync(correo);
            if (estudiante == null)
            {
                throw new NotFoundException("Estudiante no encontrado");
            }
        }

        // Obtener tutorías donde el estudiante está asignado
        var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByUsuarioAsync(estudiante.idUsuario);
        var tutoriasDto = new List<TutoriaResponseDto>();

        foreach (var te in tutoriaEstudiantes)
        {
            var tutoria = await _tutoriaRepository.GetByIdAsync(te.Tutoria_idTutoria);
            if (tutoria == null) continue;

            var tutor = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
            var horario = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);

            // Obtener todos los estudiantes de esta tutoría
            var todosEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoria.idTutoria);
            var estudiantes = new List<UsuarioResponseDto>();

            foreach (var teItem in todosEstudiantes)
            {
                var est = await _usuarioRepository.GetByIdAsync(teItem.Usuario_idUsuario);
                if (est != null)
                {
                    estudiantes.Add(new UsuarioResponseDto
                    {
                        IdUsuario = est.idUsuario,
                        Nombres = est.nombres,
                        Apellidos = est.apellidos,
                        Correo = est.correo,
                        FechaRegistro = est.fechaRegistro,
                        TipoRol = est.Rol?.tipoRol ?? "Sin rol"
                    });
                }
            }

            // Información del profesor/tutor
            var profesorDto = tutor != null ? new UsuarioResponseDto
            {
                IdUsuario = tutor.idUsuario,
                Nombres = tutor.nombres,
                Apellidos = tutor.apellidos,
                Correo = tutor.correo,
                FechaRegistro = tutor.fechaRegistro,
                TipoRol = tutor.Rol?.tipoRol ?? "Sin rol"
            } : null;

            tutoriasDto.Add(new TutoriaResponseDto
            {
                IdTutoria = tutoria.idTutoria,
                Idioma = tutoria.idioma,
                Nivel = tutoria.nivel,
                Tema = tutoria.tema,
                Modalidad = tutoria.modalidad,
                Estado = tutoria.estado,
                FechaTutoria = tutoria.fechaHora,
                UsuarioNombre = tutor?.nombres ?? "N/A",
                UsuarioApellidos = tutor?.apellidos ?? "N/A",
                UsuarioCorreo = tutor?.correo ?? "N/A",
                HorarioEspacio = horario?.espacio ?? "N/A",
                HorarioHoraInicio = horario?.horaInicio ?? DateTime.MinValue,
                HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue,
                Profesor = profesorDto,
                Estudiantes = estudiantes
            });
        }

        return tutoriasDto;
    }

    public async Task<List<TutoriaResponseDto>> GetCalendarioPorIdiomaNivelAsync(string idioma, string nivel)
    {
        // Obtener tutorías por idioma y nivel
        var tutorias = await _tutoriaRepository.GetByIdiomaNivelAsync(idioma, nivel);
        var tutoriasDto = new List<TutoriaResponseDto>();

        foreach (var tutoria in tutorias)
        {
            var tutor = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
            var horario = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);

            // Obtener estudiantes asignados
            var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoria.idTutoria);
            var estudiantes = new List<UsuarioResponseDto>();

            foreach (var te in tutoriaEstudiantes)
            {
                var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
                if (estudiante != null)
                {
                    estudiantes.Add(new UsuarioResponseDto
                    {
                        IdUsuario = estudiante.idUsuario,
                        Nombres = estudiante.nombres,
                        Apellidos = estudiante.apellidos,
                        Correo = estudiante.correo,
                        FechaRegistro = estudiante.fechaRegistro,
                        TipoRol = estudiante.Rol?.tipoRol ?? "Sin rol"
                    });
                }
            }

            // Información del profesor/tutor
            var profesorDto = tutor != null ? new UsuarioResponseDto
            {
                IdUsuario = tutor.idUsuario,
                Nombres = tutor.nombres,
                Apellidos = tutor.apellidos,
                Correo = tutor.correo,
                FechaRegistro = tutor.fechaRegistro,
                TipoRol = tutor.Rol?.tipoRol ?? "Sin rol"
            } : null;

            tutoriasDto.Add(new TutoriaResponseDto
            {
                IdTutoria = tutoria.idTutoria,
                Idioma = tutoria.idioma,
                Nivel = tutoria.nivel,
                Tema = tutoria.tema,
                Modalidad = tutoria.modalidad,
                Estado = tutoria.estado,
                FechaTutoria = tutoria.fechaHora,
                UsuarioNombre = tutor?.nombres ?? "N/A",
                UsuarioApellidos = tutor?.apellidos ?? "N/A",
                UsuarioCorreo = tutor?.correo ?? "N/A",
                HorarioEspacio = horario?.espacio ?? "N/A",
                HorarioHoraInicio = horario?.horaInicio ?? DateTime.MinValue,
                HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue,
                Profesor = profesorDto,
                Estudiantes = estudiantes
            });
        }

        return tutoriasDto;
    }
}
