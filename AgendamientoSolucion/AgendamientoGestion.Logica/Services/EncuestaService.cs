using AgendamientoGestion.Logica.Correo;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AgendamientoGestion.Persistencia.Interfaces;

namespace AgendamientoGestion.Logica.Services;

public class EncuestaService : IEncuestaService
{
    private readonly IEncuestaRepository _encuestaRepository;
    private readonly ITutoriaRepository _tutoriaRepository;
    private readonly ITutoriaEstudianteRepository _tutoriaEstudianteRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICorreoService _correoService;
    private readonly IPlantillasCorreo _plantillas;
    private readonly EncuestaSettings _settings;
    private readonly ILogger<EncuestaService> _logger;

    public EncuestaService(
        IEncuestaRepository encuestaRepository,
        ITutoriaRepository tutoriaRepository,
        ITutoriaEstudianteRepository tutoriaEstudianteRepository,
        IUsuarioRepository usuarioRepository,
        ICorreoService correoService,
        IPlantillasCorreo plantillas,
        IOptions<EncuestaSettings> settings,
        ILogger<EncuestaService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _encuestaRepository = encuestaRepository;
        _tutoriaRepository = tutoriaRepository;
        _tutoriaEstudianteRepository = tutoriaEstudianteRepository;
        _usuarioRepository = usuarioRepository;
        _correoService = correoService;
        _plantillas = plantillas;
    }

    public async Task<EnvioEncuestasResultadoDto> EnviarEncuestasAsync(int tutoriaId)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
        if (tutoria == null)
            throw new NotFoundException("Tutoría no encontrada");

        var resultado = new EnvioEncuestasResultadoDto();
        var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);

        // Idempotencia: PUT /Tutoria/{id}/finalizar ya envía las encuestas y la app MAUI además llama a
        // POST /Encuesta/enviar/{id}. No se crea una segunda encuesta para el mismo estudiante y tutoría.
        var existentes = await _encuestaRepository.GetByTutoriaIdAsync(tutoriaId);
        var correosConEncuesta = new HashSet<string>(
            existentes.Where(e => !string.IsNullOrWhiteSpace(e.estudianteCorreo))
                      .Select(e => e.estudianteCorreo!.Trim()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var te in tutoriaEstudiantes)
        {
            var estudiante = te.Usuario ?? await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
            if (estudiante == null) continue;

            if (!correosConEncuesta.Add(estudiante.correo.Trim()))
            {
                resultado.YaExistentes++;
                continue;
            }

            var token = Guid.NewGuid().ToString("N");

            var encuesta = new EncuestaSatisfaccion
            {
                tokenRespuesta = token,
                tokenUsado = false,
                estudianteNombre = $"{estudiante.nombres} {estudiante.apellidos}",
                estudianteCorreo = estudiante.correo,
                Tutoria_idTutoria = tutoriaId
            };

            await _encuestaRepository.CreateAsync(encuesta);
            resultado.Enviadas++;

            // Enlace a la página pública de encuesta en React (configurable: Encuesta:UrlBase)
            var enlace = _settings.ConstruirEnlace(token);

            var contenido = _plantillas.Renderizar(new EncuestaSatisfaccionCorreo(
                estudiante.nombres, tutoria.idioma, tutoria.nivel, tutoria.tema, enlace));

            try
            {
                _correoService.Enviar(MensajeCorreo.Desde(contenido, estudiante.correo, $"{estudiante.nombres} {estudiante.apellidos}".Trim()));
            }
            catch (Exception ex)
            {
                // La encuesta queda creada; el fallo de SMTP no debe tumbar el flujo.
                _logger.LogError(ex, "Error enviando la encuesta {IdEncuesta} de la tutoría {TutoriaId} al estudiante {EstudianteId}",
                    encuesta.idEncuesta, tutoriaId, estudiante.idUsuario);
            }
        }

        return resultado;
    }

    public async Task<EncuestaResponseDto> VerificarTokenAsync(string token)
    {
        var encuesta = await _encuestaRepository.GetByTokenAsync(token);
        if (encuesta == null)
            throw new NotFoundException("Encuesta no encontrada o token inválido");

        if (encuesta.tokenUsado)
            throw new BusinessException("Esta encuesta ya fue respondida");

        return MapToDto(encuesta);
    }

    public async Task<EncuestaResponseDto> ResponderEncuestaAsync(EncuestaCreateDto dto)
    {
        var encuesta = await _encuestaRepository.GetByTokenAsync(dto.Token);
        if (encuesta == null)
            throw new NotFoundException("Encuesta no encontrada o token inválido");

        if (encuesta.tokenUsado)
            throw new BusinessException("Esta encuesta ya fue respondida");

        encuesta.calificacion = dto.Calificacion;
        encuesta.comentario = dto.Comentario;
        encuesta.fechaRespuesta = DateTime.Now;
        encuesta.tokenUsado = true;

        await _encuestaRepository.UpdateAsync(encuesta);
        return MapToDto(encuesta);
    }

    public async Task<List<EncuestaResponseDto>> GetByTutoriaIdAsync(int tutoriaId)
    {
        var encuestas = await _encuestaRepository.GetByTutoriaIdAsync(tutoriaId);
        return encuestas.Select(MapToDto).ToList();
    }

    public async Task<EncuestaEstadisticasDto> GetEstadisticasAsync()
    {
        var todas = await _encuestaRepository.GetAllAsync();
        var respondidas = todas.Where(e => e.tokenUsado).ToList();

        double promedio = respondidas.Count > 0
            ? respondidas.Average(e => e.calificacion)
            : 0;

        double tasa = todas.Count > 0
            ? Math.Round((double)respondidas.Count / todas.Count * 100, 1)
            : 0;

        return new EncuestaEstadisticasDto
        {
            TotalEnviadas = todas.Count,
            TotalRespondidas = respondidas.Count,
            PromedioCalificacion = Math.Round(promedio, 1),
            TasaRespuesta = tasa
        };
    }

    public async Task<List<EncuestasPendientesDto>> GetPendientesAsync()
    {
        var pendientes = await _encuestaRepository.GetPendientesPorTutoriaAsync();
        return pendientes.Select(p => new EncuestasPendientesDto
        {
            TutoriaId = p.Tutoria.idTutoria,
            Tema = p.Tutoria.tema,
            FechaHora = p.Tutoria.fechaHora,
            Enviadas = p.Enviadas,
            SinResponder = p.SinResponder
        }).ToList();
    }

    private EncuestaResponseDto MapToDto(EncuestaSatisfaccion encuesta)
    {
        return new EncuestaResponseDto
        {
            IdEncuesta = encuesta.idEncuesta,
            Calificacion = encuesta.calificacion,
            Comentario = encuesta.comentario,
            FechaRespuesta = encuesta.fechaRespuesta,
            EstudianteNombre = encuesta.estudianteNombre,
            EstudianteCorreo = encuesta.estudianteCorreo,
            TokenUsado = encuesta.tokenUsado,
            TutoriaId = encuesta.Tutoria_idTutoria
        };
    }
}
