using AgendamientoGestion.Entidades.Constants;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Seguridad;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendamientoGestion.Logica.Services;

public class TutoriaService : ITutoriaService
{
    public const string MensajeAsignadoInactivo = "El usuario asignado tiene la cuenta desactivada";

    private readonly ITutoriaRepository _tutoriaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IHorarioRepository _horarioRepository;
    private readonly INotificacionService _notificacionService;
    private readonly ITutoriaEstudianteRepository _tutoriaEstudianteRepository;
    private readonly IRetroalimentacionRepository _retroalimentacionRepository;
    private readonly IEncuestaService _encuestaService;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger<TutoriaService> _logger;

    public TutoriaService(
        ITutoriaRepository tutoriaRepository,
        IUsuarioRepository usuarioRepository,
        IHorarioRepository horarioRepository,
        INotificacionService notificacionService,
        ITutoriaEstudianteRepository tutoriaEstudianteRepository,
        IRetroalimentacionRepository retroalimentacionRepository,
        IEncuestaService encuestaService,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger<TutoriaService> logger)
    {
        _tutoriaRepository = tutoriaRepository;
        _usuarioRepository = usuarioRepository;
        _horarioRepository = horarioRepository;
        _notificacionService = notificacionService;
        _tutoriaEstudianteRepository = tutoriaEstudianteRepository;
        _retroalimentacionRepository = retroalimentacionRepository;
        _encuestaService = encuestaService;
        _unidadDeTrabajo = unidadDeTrabajo;
        _logger = logger;
    }

    // ---------------------------------------------------------------------
    // Crear (R4 validaciones, R9 estudiantes atómicos)
    // ---------------------------------------------------------------------

    public async Task<TutoriaResponseDto> CreateTutoriaAsync(TutoriaCreateDto tutoriaDto, Solicitante solicitante)
    {
        // Paridad MAUI: el Tutor no agenda. Solo Admin/Aux (cualquier horario) y Docente (sus horarios).
        if (!solicitante.EsGestion && !solicitante.EsDocente)
        {
            throw new ForbiddenException("Solo un Docente, Administrador o Auxiliar puede agendar tutorías");
        }

        var asignado = await _usuarioRepository.GetByIdAsync(tutoriaDto.UsuarioId);
        if (asignado == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        if (!Roles.Es(asignado.Rol?.tipoRol, Roles.Docente) && !Roles.Es(asignado.Rol?.tipoRol, Roles.Tutor))
        {
            throw new ValidationException("El usuario asignado debe ser Docente o Tutor");
        }

        if (!asignado.activo)
        {
            throw new ValidationException(MensajeAsignadoInactivo);
        }

        var horario = await _horarioRepository.GetByIdAsync(tutoriaDto.HorarioId);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        if (solicitante.EsDocente && horario.Usuario_idUsuario != solicitante.UsuarioId)
        {
            throw new ForbiddenException("Solo puede agendar tutorías en sus propios horarios");
        }

        if (!EstadosHorario.EsDisponible(horario.estado))
        {
            throw new ConflictException("El horario ya no está disponible");
        }

        var fechaHora = NormalizarFechaTutoria(tutoriaDto.FechaTutoria, horario);
        var cupos = Cupos.ValidarTutoria(tutoriaDto.Cupos, obligatorio: true);

        // Validar estudiantes y cupo ANTES de escribir nada.
        var estudiantes = await ValidarEstudiantesNuevosAsync(tutoriaDto.EstudianteIds ?? new List<int>(), tutoriaId: null);
        VerificarCupo(cupos, horario, actuales: 0, nuevos: estudiantes.Count);

        Tutoria tutoriaCreada;
        await using (var transaccion = await _unidadDeTrabajo.IniciarTransaccionAsync())
        {
            // UPDATE condicional: si otra petición reservó el horario entre la lectura y aquí → 409.
            if (!await _horarioRepository.ReservarAsync(horario.idHorario))
            {
                throw new ConflictException("El horario ya no está disponible");
            }

            tutoriaCreada = await _tutoriaRepository.CreateAsync(new Tutoria
            {
                idioma = tutoriaDto.Idioma,
                nivel = tutoriaDto.Nivel,
                tema = tutoriaDto.Tema,
                modalidad = tutoriaDto.Modalidad,
                estado = tutoriaDto.Estado,
                fechaHora = fechaHora,
                Usuario_idUsuario = asignado.idUsuario,
                Horario_idHorario = horario.idHorario,
                cupos = cupos
            });

            if (estudiantes.Count > 0)
            {
                await _tutoriaEstudianteRepository.CreateRangeAsync(estudiantes.Select(e => new TutoriaEstudiante
                {
                    Tutoria_idTutoria = tutoriaCreada.idTutoria,
                    Usuario_idUsuario = e.idUsuario
                }));
            }

            await transaccion.CommitAsync();
        }

        // Notificación automática (a los estudiantes asignados o, si no hay, al tutor). Un fallo de SMTP
        // no deshace la creación.
        try
        {
            await _notificacionService.EnviarNotificacionTutoriaPorEmailAsync(tutoriaCreada.idTutoria, "asignacion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando la notificación automática de la tutoría {TutoriaId}", tutoriaCreada.idTutoria);
        }

        tutoriaCreada.Usuario = asignado;
        tutoriaCreada.Horario = horario;
        return await MapConEstudiantesAsync(tutoriaCreada);
    }

    /// <summary>
    /// La fecha debe caer dentro de [fechaInicio, fechaFin] del horario. Si llega sin hora (00:00, como envía
    /// MAUI a veces) se toma la hora de inicio del horario; si trae hora, debe estar en [horaInicio, horaFin).
    /// </summary>
    private static DateTime NormalizarFechaTutoria(DateTime fecha, Horario horario)
    {
        var dia = fecha.Date;
        if (dia < horario.fechaInicio.Date || dia > horario.fechaFin.Date)
        {
            throw new ValidationException(
                $"La fecha de la tutoría ({dia:dd/MM/yyyy}) debe estar dentro del horario ({horario.fechaInicio:dd/MM/yyyy} - {horario.fechaFin:dd/MM/yyyy})");
        }

        var inicio = horario.horaInicio.TimeOfDay;
        var fin = horario.horaFin.TimeOfDay;

        if (fecha.TimeOfDay == TimeSpan.Zero)
        {
            return dia + inicio;
        }

        // Si el horario cruza la medianoche (fin <= inicio) no se valida la hora.
        if (fin > inicio && (fecha.TimeOfDay < inicio || fecha.TimeOfDay >= fin))
        {
            throw new ValidationException($"La hora de la tutoría debe estar entre {inicio:hh\\:mm} y {fin:hh\\:mm}");
        }

        return fecha;
    }

    // ---------------------------------------------------------------------
    // Lectura con regla de propiedad (R2)
    // ---------------------------------------------------------------------

    public async Task<TutoriaResponseDto> GetTutoriaByIdAsync(int id, Solicitante solicitante)
    {
        var tutoria = await ObtenerConAccesoAsync(id, solicitante, AccionTutoria.Ver);
        return await MapConEstudiantesAsync(tutoria);
    }

    public async Task AsegurarAccesoAsync(int tutoriaId, Solicitante solicitante, AccionTutoria accion)
    {
        await ObtenerConAccesoAsync(tutoriaId, solicitante, accion);
    }

    private async Task<Tutoria> ObtenerConAccesoAsync(int id, Solicitante solicitante, AccionTutoria accion)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(id);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        ReglasAccesoTutoria.Asegurar(solicitante, tutoria, accion);
        return tutoria;
    }

    public async Task<List<TutoriaResponseDto>> GetAllTutoriasAsync()
    {
        var tutorias = await _tutoriaRepository.GetAllAsync();
        return await MapListaAsync(tutorias);
    }

    public async Task<TutoriaResponseDto> UpdateTutoriaAsync(int id, TutoriaCreateDto tutoriaDto)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(id);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        var tutor = await _usuarioRepository.GetByIdAsync(tutoriaDto.UsuarioId);
        if (tutor == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Solo al REASIGNAR: editar otros datos de una tutoría cuyo asignado ya está inactivo sigue permitido.
        if (tutor.idUsuario != tutoria.Usuario_idUsuario && !tutor.activo)
        {
            throw new ValidationException(MensajeAsignadoInactivo);
        }

        var horario = await _horarioRepository.GetByIdAsync(tutoriaDto.HorarioId);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        // Datos anteriores para detectar cambios en la notificación
        var tutoriaAnterior = new TutoriaResponseDto
        {
            IdTutoria = tutoria.idTutoria,
            Idioma = tutoria.idioma,
            Nivel = tutoria.nivel,
            Tema = tutoria.tema,
            Modalidad = tutoria.modalidad,
            Estado = tutoria.estado,
            FechaTutoria = tutoria.fechaHora,
            UsuarioId = tutoria.Usuario_idUsuario,
            HorarioId = tutoria.Horario_idHorario,
            UsuarioNombre = tutoria.Usuario?.nombres ?? "",
            UsuarioApellidos = tutoria.Usuario?.apellidos ?? "",
            HorarioEspacio = tutoria.Horario?.espacio ?? ""
        };

        // Cupos opcional en PUT: ausente = conserva. No puede quedar por debajo de los ya inscritos.
        var nuevosCupos = Cupos.ValidarTutoria(tutoriaDto.Cupos, obligatorio: false);
        if (nuevosCupos != null)
        {
            var inscritos = await _tutoriaEstudianteRepository.CountByTutoriaAsync(tutoria.idTutoria);
            if (nuevosCupos.Value < inscritos)
            {
                throw new ConflictException(
                    $"No se pueden fijar {nuevosCupos.Value} cupos: la tutoría ya tiene {inscritos} estudiantes inscritos");
            }

            tutoria.cupos = nuevosCupos;
        }

        tutoria.idioma = tutoriaDto.Idioma;
        tutoria.nivel = tutoriaDto.Nivel;
        tutoria.tema = tutoriaDto.Tema;
        tutoria.modalidad = tutoriaDto.Modalidad;
        tutoria.estado = tutoriaDto.Estado;
        tutoria.fechaHora = tutoriaDto.FechaTutoria;
        tutoria.Usuario_idUsuario = tutor.idUsuario;
        tutoria.Usuario = tutor;
        tutoria.Horario_idHorario = horario.idHorario;
        tutoria.Horario = horario;

        await _tutoriaRepository.UpdateAsync(tutoria);

        try
        {
            await _notificacionService.EnviarNotificacionCambioPorEmailAsync(tutoria.idTutoria, "Cambio realizado en la tutoría", tutoriaAnterior);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando la notificación de cambio de la tutoría {TutoriaId}", tutoria.idTutoria);
        }

        return MapTutoria(tutoria, tutor, horario, new List<UsuarioResponseDto>(), sinDatos: string.Empty);
    }

    public async Task<bool> DeleteTutoriaAsync(int id)
    {
        // Borra en una transacción Informe, Retroalimentacion, TutoriaEstudiante y EncuestaSatisfaccion
        // (antes faltaba EncuestaSatisfaccion → violación de FK NO ACTION → HTTP 500).
        var eliminado = await _tutoriaRepository.DeleteWithDependenciesAsync(id);
        if (!eliminado)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        return true;
    }

    // ---------------------------------------------------------------------
    // Estudiantes (R2 propiedad, R5 cupos)
    // ---------------------------------------------------------------------

    public async Task<bool> AgregarEstudiantesATutoriaAsync(int tutoriaId, AgregarEstudiantesTutoriaDto estudiantesDto, Solicitante solicitante)
    {
        var tutoria = await ObtenerConAccesoAsync(tutoriaId, solicitante, AccionTutoria.GestionarEstudiantes);

        List<Usuario> nuevos;
        await using (var transaccion = await _unidadDeTrabajo.IniciarTransaccionAsync())
        {
            // Serializa las altas sobre ESTA tutoría: el recuento de cupo y el INSERT no pueden intercalarse
            // con otra petición concurrente (evita superar el cupo).
            await _tutoriaRepository.BloquearAsync(tutoriaId);

            // 1) Validar TODOS los estudiantes antes de escribir nada (todo o nada).
            nuevos = await ValidarEstudiantesNuevosAsync(estudiantesDto.EstudianteIds, tutoriaId);
            if (nuevos.Count == 0)
            {
                return false;
            }

            // 2) Cupo del horario = máximo de estudiantes de la tutoría.
            var actuales = await _tutoriaEstudianteRepository.CountByTutoriaAsync(tutoriaId);
            VerificarCupo(tutoria.cupos, tutoria.Horario, actuales, nuevos.Count);

            // 3) Insertar todas las asignaciones en un único SaveChanges.
            await _tutoriaEstudianteRepository.CreateRangeAsync(nuevos.Select(e => new TutoriaEstudiante
            {
                Tutoria_idTutoria = tutoriaId,
                Usuario_idUsuario = e.idUsuario
            }));

            await transaccion.CommitAsync();
        }

        // 4) Notificar después de confirmar; un fallo de correo no deshace la asignación.
        var tutoriaDto = MapTutoria(tutoria, tutoria.Usuario, tutoria.Horario, new List<UsuarioResponseDto>());
        foreach (var estudiante in nuevos)
        {
            try
            {
                await _notificacionService.EnviarNotificacionEstudianteTutoriaAsync(tutoriaDto, MapEstudiante(estudiante), "asignacion");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando la notificación de asignación al estudiante {EstudianteId} (tutoría {TutoriaId})",
                    estudiante.idUsuario, tutoriaId);
            }
        }

        return true;
    }

    /// <summary>
    /// Comprueba que cada id existe y es Estudiante (404/400). Devuelve solo los que aún no están asignados
    /// a <paramref name="tutoriaId"/> (null = tutoría nueva).
    /// </summary>
    private async Task<List<Usuario>> ValidarEstudiantesNuevosAsync(IEnumerable<int> estudianteIds, int? tutoriaId)
    {
        var resultado = new List<Usuario>();
        foreach (var estudianteId in estudianteIds.Distinct())
        {
            var estudiante = await _usuarioRepository.GetByIdAsync(estudianteId);
            if (estudiante == null)
            {
                throw new NotFoundException($"Estudiante con ID {estudianteId} no encontrado");
            }

            if (!Roles.Es(estudiante.Rol?.tipoRol, Roles.Estudiante))
            {
                throw new ValidationException($"El usuario con ID {estudianteId} no tiene el rol de Estudiante");
            }

            if (tutoriaId == null || !await _tutoriaEstudianteRepository.ExistsAsync(tutoriaId.Value, estudiante.idUsuario))
            {
                // Solo se rechaza al AGREGAR: un estudiante ya inscrito que luego se desactiva no rompe la operación.
                if (!estudiante.activo)
                {
                    throw new ValidationException($"El estudiante con ID {estudianteId} tiene la cuenta desactivada");
                }

                resultado.Add(estudiante);
            }
        }

        return resultado;
    }

    /// <summary>
    /// Límite = Tutoria.cupos; si es null (tutorías antiguas) los cupos del horario; si tampoco son numéricos, sin límite.
    /// </summary>
    private void VerificarCupo(int? cuposTutoria, Horario? horario, int actuales, int nuevos)
    {
        if (nuevos == 0) return;

        var cupo = Cupos.Efectivos(cuposTutoria, horario?.cupos);
        if (cupo == null)
        {
            _logger.LogWarning("La tutoría (horario {HorarioId}) no tiene cupos numéricos ('{Cupos}'): se asignan estudiantes sin límite",
                horario?.idHorario, horario?.cupos);
            return;
        }

        if (actuales + nuevos > cupo.Value)
        {
            throw new ConflictException(
                $"Cupo completo: {actuales} de {cupo.Value} ocupados (se intentaron agregar {nuevos})");
        }
    }

    public async Task<List<UsuarioResponseDto>> GetEstudiantesByTutoriaAsync(int tutoriaId, Solicitante solicitante)
    {
        await ObtenerConAccesoAsync(tutoriaId, solicitante, AccionTutoria.Ver);
        return await GetEstudiantesDtoAsync(tutoriaId);
    }

    public async Task<bool> EliminarEstudianteDeTutoriaAsync(int tutoriaId, int estudianteId, Solicitante solicitante)
    {
        await ObtenerConAccesoAsync(tutoriaId, solicitante, AccionTutoria.GestionarEstudiantes);

        var estudiante = await _usuarioRepository.GetByIdAsync(estudianteId);
        if (estudiante == null)
        {
            throw new NotFoundException("Estudiante no encontrado");
        }

        if (!await _tutoriaEstudianteRepository.ExistsAsync(tutoriaId, estudianteId))
        {
            throw new NotFoundException("El estudiante no está asignado a esta tutoría");
        }

        return await _tutoriaEstudianteRepository.DeleteByTutoriaAndEstudianteAsync(tutoriaId, estudianteId);
    }

    // ---------------------------------------------------------------------
    // Calendarios y filtros (R1, R7)
    // ---------------------------------------------------------------------

    public async Task<List<TutoriaResponseDto>> GetCalendarioPorDocenteAsync(string nombre, string apellidos, string correo)
    {
        var docente = await _usuarioRepository.GetByNombreApellidosCorreoAsync(nombre, apellidos, correo)
                      ?? await _usuarioRepository.GetByEmailAsync(correo);
        if (docente == null)
        {
            throw new NotFoundException("Docente no encontrado");
        }

        if (!Roles.Es(docente.Rol?.tipoRol, Roles.Docente) && !Roles.Es(docente.Rol?.tipoRol, Roles.Tutor))
        {
            throw new ValidationException("El usuario especificado no es un docente/tutor");
        }

        var tutorias = await _tutoriaRepository.GetByUsuarioAsync(docente.idUsuario);
        return await MapListaAsync(tutorias);
    }

    public async Task<List<TutoriaResponseDto>> GetCalendarioPorEstudianteAsync(string nombre, string apellidos, string correo)
    {
        var estudiante = await _usuarioRepository.GetByNombreApellidosCorreoAsync(nombre, apellidos, correo)
                         ?? await _usuarioRepository.GetByEmailAsync(correo);
        if (estudiante == null)
        {
            throw new NotFoundException("Estudiante no encontrado");
        }

        // 3 consultas en total (inscripciones, tutorías, estudiantes) en lugar de 2 por inscripción.
        var asignaciones = await _tutoriaEstudianteRepository.GetByUsuarioAsync(estudiante.idUsuario);
        var tutorias = (await _tutoriaRepository.GetByIdsAsync(asignaciones.Select(te => te.Tutoria_idTutoria).Distinct().ToList()))
            .ToDictionary(t => t.idTutoria);

        // Mismo orden que las inscripciones (como antes).
        var ordenadas = asignaciones
            .Where(te => tutorias.ContainsKey(te.Tutoria_idTutoria))
            .Select(te => tutorias[te.Tutoria_idTutoria])
            .ToList();
        return await MapListaAsync(ordenadas);
    }

    public async Task<List<TutoriaResponseDto>> GetCalendarioPorIdiomaNivelAsync(string idioma, string nivel)
    {
        var tutorias = await _tutoriaRepository.GetByIdiomaNivelAsync(idioma, nivel);
        return await MapListaAsync(tutorias);
    }

    public async Task<List<TutoriaResponseDto>> GetTutoriasByEstadoAsync(string estado)
    {
        var tutorias = await _tutoriaRepository.GetByEstadoAsync(estado);
        return await MapListaAsync(tutorias);
    }

    /// <summary>Tutorías ASIGNADAS al usuario. El estado se interpreta como grupo de sinónimos (R7).</summary>
    public async Task<List<TutoriaResponseDto>> GetTutoriasByEstadoAndUsuarioAsync(string estado, int usuarioId)
    {
        var tutorias = await _tutoriaRepository.GetByEstadosAndUsuarioAsync(EstadosTutoria.SinonimosLower(estado), usuarioId);
        return await MapListaAsync(tutorias);
    }

    /// <summary>Tutorías AGENDADAS en horarios del usuario (R1). Estado opcional, como grupo de sinónimos.</summary>
    public async Task<List<TutoriaResponseDto>> GetTutoriasAgendadasPorUsuarioAsync(int usuarioId, string? estado)
    {
        var estados = string.IsNullOrWhiteSpace(estado) ? null : EstadosTutoria.SinonimosLower(estado);
        var tutorias = await _tutoriaRepository.GetByDuenoHorarioAsync(usuarioId, estados);
        return await MapListaAsync(tutorias);
    }

    // ---------------------------------------------------------------------
    // Finalizar (R2, R8)
    // ---------------------------------------------------------------------

    public async Task<TutoriaResponseDto> FinalizarTutoriaAsync(int tutoriaId, FinalizarTutoriaDto? datos, Solicitante solicitante)
    {
        var tutoria = await ObtenerConAccesoAsync(tutoriaId, solicitante, AccionTutoria.Finalizar);

        // Acepta ambos sinónimos ("Finalizada"/"Completada") como ya finalizada.
        if (EstadosTutoria.EsFinalizada(tutoria.estado))
        {
            throw new ValidationException("La tutoría ya está finalizada");
        }

        if (EstadosTutoria.EsCancelada(tutoria.estado))
        {
            throw new ValidationException("No se puede finalizar una tutoría cancelada");
        }

        if (datos?.Calificacion == null && !string.IsNullOrWhiteSpace(datos?.Comentario))
        {
            throw new ValidationException("Para registrar la retroalimentación se requiere la calificación (1 a 5)");
        }

        await using (var transaccion = await _unidadDeTrabajo.IniciarTransaccionAsync())
        {
            // UPDATE condicional: dos "finalizar" simultáneos no pueden pasar ambos (ni duplicar la retroalimentación).
            if (!await _tutoriaRepository.MarcarFinalizadaAsync(tutoriaId))
            {
                throw new ValidationException("La tutoría ya está finalizada");
            }

            if (datos?.Calificacion is decimal calificacion)
            {
                if (await _retroalimentacionRepository.ExistsAsync(tutoriaId, solicitante.UsuarioId))
                {
                    throw new ConflictException("Ya registró una retroalimentación para esta tutoría");
                }

                await _retroalimentacionRepository.CreateAsync(new Retroalimentacion
                {
                    comentario = datos.Comentario?.Trim() ?? string.Empty,
                    calificacion = calificacion,
                    fecha = DateTime.Now,
                    Tutoria_idTutoria = tutoriaId,
                    Usuario_idUsuario = solicitante.UsuarioId
                });
            }

            await transaccion.CommitAsync();
        }

        // Encuestas de satisfacción (idempotente). Un fallo no revierte la finalización.
        try
        {
            await _encuestaService.EnviarEncuestasAsync(tutoriaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando encuestas de satisfacción de la tutoría {TutoriaId}", tutoriaId);
        }

        return await MapConEstudiantesAsync(tutoria);
    }

    // ---------------------------------------------------------------------
    // Mapeo
    // ---------------------------------------------------------------------

    /// <summary>
    /// Listas: los estudiantes de TODAS las tutorías se cargan en una sola consulta (antes una por tutoría: N+1,
    /// ~150 ms por ida y vuelta en producción). Se conserva el orden de la lista y el de los estudiantes.
    /// </summary>
    private async Task<List<TutoriaResponseDto>> MapListaAsync(IEnumerable<Tutoria> tutorias)
    {
        var lista = tutorias as IList<Tutoria> ?? tutorias.ToList();
        var ids = lista.Select(t => t.idTutoria).Distinct().ToList();
        var porTutoria = (await _tutoriaEstudianteRepository.GetByTutoriasAsync(ids))
            .Where(te => te.Usuario != null)
            .GroupBy(te => te.Tutoria_idTutoria)
            .ToDictionary(g => g.Key, g => g.Select(te => te.Usuario).ToList());

        return lista
            .Select(t => MapTutoria(t, t.Usuario, t.Horario,
                porTutoria.TryGetValue(t.idTutoria, out var usuarios)
                    ? usuarios.Select(MapEstudiante).ToList()
                    : new List<UsuarioResponseDto>()))
            .ToList();
    }

    /// <summary>
    /// El repositorio de tutorías ya incluye Usuario, Horario y Horario.Usuario, y el de TutoriaEstudiante
    /// incluye Usuario+Rol. Queda 1 consulta de estudiantes por tutoría (N+1 parcial, ver propuestas en docs/api).
    /// </summary>
    private async Task<TutoriaResponseDto> MapConEstudiantesAsync(Tutoria tutoria)
    {
        var estudiantes = await GetEstudiantesDtoAsync(tutoria.idTutoria);
        return MapTutoria(tutoria, tutoria.Usuario, tutoria.Horario, estudiantes);
    }

    private async Task<List<UsuarioResponseDto>> GetEstudiantesDtoAsync(int tutoriaId)
    {
        var asignaciones = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);
        return asignaciones
            .Where(te => te.Usuario != null)
            .Select(te => MapEstudiante(te.Usuario))
            .ToList();
    }

    private static UsuarioResponseDto MapEstudiante(Usuario usuario) =>
        UsuarioService.Map(usuario, usuario.Rol?.tipoRol);

    private static TutoriaResponseDto MapTutoria(
        Tutoria tutoria, Usuario? tutor, Horario? horario, List<UsuarioResponseDto> estudiantes, string sinDatos = "N/A") => new()
    {
        IdTutoria = tutoria.idTutoria,
        Idioma = tutoria.idioma,
        Nivel = tutoria.nivel,
        Tema = tutoria.tema,
        Modalidad = tutoria.modalidad,
        Estado = tutoria.estado,
        FechaTutoria = tutoria.fechaHora,
        UsuarioId = tutoria.Usuario_idUsuario,
        HorarioId = tutoria.Horario_idHorario,
        // Asignado eliminado (Usuario_idUsuario null) → null en id y nombres: la UI muestra "Sin asignar (usuario eliminado)".
        UsuarioNombre = tutoria.Usuario_idUsuario == null ? null : tutor?.nombres ?? sinDatos,
        UsuarioApellidos = tutoria.Usuario_idUsuario == null ? null : tutor?.apellidos ?? sinDatos,
        UsuarioCorreo = tutoria.Usuario_idUsuario == null ? null : tutor?.correo ?? sinDatos,
        HorarioEspacio = horario?.espacio ?? sinDatos,
        HorarioHoraInicio = horario?.horaInicio ?? DateTime.MinValue,
        HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue,
        HorarioUsuarioId = horario?.Usuario_idUsuario,
        HorarioUsuarioNombre = horario is { Usuario_idUsuario: null } ? null : horario?.Usuario?.nombres ?? sinDatos,
        HorarioUsuarioApellidos = horario is { Usuario_idUsuario: null } ? null : horario?.Usuario?.apellidos ?? sinDatos,
        HorarioTitulo = horario?.titulo ?? sinDatos,
        HorarioCupos = Cupos.Interpretar(horario?.cupos),
        Cupos = Cupos.Efectivos(tutoria.cupos, horario?.cupos),
        Estudiantes = estudiantes
    };
}
