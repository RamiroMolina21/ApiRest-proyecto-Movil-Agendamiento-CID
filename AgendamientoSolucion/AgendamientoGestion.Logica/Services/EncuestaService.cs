using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.Interfaces;

namespace AgendamientoGestion.Logica.Services;

public class EncuestaService : IEncuestaService
{
    private readonly IEncuestaRepository _encuestaRepository;
    private readonly ITutoriaRepository _tutoriaRepository;
    private readonly ITutoriaEstudianteRepository _tutoriaEstudianteRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICorreoService _correoService;

    public EncuestaService(
        IEncuestaRepository encuestaRepository,
        ITutoriaRepository tutoriaRepository,
        ITutoriaEstudianteRepository tutoriaEstudianteRepository,
        IUsuarioRepository usuarioRepository,
        ICorreoService correoService)
    {
        _encuestaRepository = encuestaRepository;
        _tutoriaRepository = tutoriaRepository;
        _tutoriaEstudianteRepository = tutoriaEstudianteRepository;
        _usuarioRepository = usuarioRepository;
        _correoService = correoService;
    }

    public async Task EnviarEncuestasAsync(int tutoriaId)
    {
        var tutoria = await _tutoriaRepository.GetByIdAsync(tutoriaId);
        if (tutoria == null)
            throw new NotFoundException("Tutoría no encontrada");

        var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoriaId);

        foreach (var te in tutoriaEstudiantes)
        {
            var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
            if (estudiante == null) continue;

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

            // Enlace a la página pública de encuesta en React
            var enlace = $"http://localhost:5173/encuesta?token={token}";

            var htmlCuerpo = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #1a73e8;'>Encuesta de Satisfacción - Tutoría</h2>
                    <p>Hola <strong>{estudiante.nombres}</strong>,</p>
                    <p>Has participado en una tutoría de <strong>{tutoria.idioma} - {tutoria.nivel}</strong>
                       sobre el tema <strong>{tutoria.tema}</strong>.</p>
                    <p>Tu opinión es muy importante para nosotros. Por favor toma un momento para calificar tu experiencia:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{enlace}'
                           style='background-color: #1a73e8; color: white; padding: 14px 28px;
                                  text-decoration: none; border-radius: 6px; font-size: 16px;'>
                            ✏️ Responder Encuesta
                        </a>
                    </div>
                    <p style='color: #666; font-size: 13px;'>
                        Si el botón no funciona, copia y pega este enlace en tu navegador:<br/>
                        <a href='{enlace}'>{enlace}</a>
                    </p>
                    <hr style='border: 1px solid #eee;'/>
                    <p style='color: #999; font-size: 12px;'>Sistema de Tutorías - CECAR</p>
                </div>";

            try
            {
                _correoService.EnviarCorreo(
                    estudiante.correo,
                    "Encuesta de satisfacción - Tutoría",
                    htmlCuerpo
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando encuesta a {estudiante.correo}: {ex.Message}");
            }
        }
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
