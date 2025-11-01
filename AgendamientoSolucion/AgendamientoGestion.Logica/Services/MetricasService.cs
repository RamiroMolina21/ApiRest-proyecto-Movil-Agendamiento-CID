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

namespace AgendamientoGestion.Logica.Services
{
    public class MetricasService : IMetricasService
    {
        private readonly ITutoriaRepository _tutoriaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IHorarioRepository _horarioRepository;
        private readonly IRetroalimentacionRepository _retroalimentacionRepository;
        private readonly ITutoriaEstudianteRepository _tutoriaEstudianteRepository;
        private readonly IInformeRepository _informeRepository;

        public MetricasService(
            ITutoriaRepository tutoriaRepository,
            IUsuarioRepository usuarioRepository,
            IHorarioRepository horarioRepository,
            IRetroalimentacionRepository retroalimentacionRepository,
            ITutoriaEstudianteRepository tutoriaEstudianteRepository,
            IInformeRepository informeRepository)
        {
            _tutoriaRepository = tutoriaRepository;
            _usuarioRepository = usuarioRepository;
            _horarioRepository = horarioRepository;
            _retroalimentacionRepository = retroalimentacionRepository;
            _tutoriaEstudianteRepository = tutoriaEstudianteRepository;
            _informeRepository = informeRepository;
        }

        public async Task<MetricasResponseDto> GetMetricasGeneralesAsync()
        {
            try
            {
                var tutorias = await _tutoriaRepository.GetAllAsync();
                var usuarios = await _usuarioRepository.GetAllAsync();
                var horarios = await _horarioRepository.GetAllAsync();
                var retroalimentaciones = await _retroalimentacionRepository.GetAllAsync();

                var metricas = new MetricasResponseDto
                {
                    TotalTutorias = tutorias.Count,
                    TutoriasCompletadas = tutorias.Count(t => t.estado == "Completada"),
                    TutoriasCanceladas = tutorias.Count(t => t.estado == "Cancelada"),
                    TutoriasProgramadas = tutorias.Count(t => t.estado == "Programada"),
                    PromedioCalificacion = retroalimentaciones.Any() ? retroalimentaciones.Average(r => r.calificacion) : 0,
                    TotalUsuarios = usuarios.Count,
                    TotalDocentes = usuarios.Count(u => u.Rol?.tipoRol == "Docente"),
                    TotalEstudiantes = usuarios.Count(u => u.Rol?.tipoRol == "Estudiante"),
                    TotalHorarios = horarios.Count,
                    HorariosDisponibles = horarios.Count(h => h.estado == "Disponible")
                };

                return metricas;
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Error al obtener métricas generales: {ex.Message}");
            }
        }

        public async Task<List<MetricasTutoriasDetalladasDto>> GetMetricasTutoriasAsync()
        {
            try
            {
                var tutorias = await _tutoriaRepository.GetAllAsync();
                var metricas = new List<MetricasTutoriasDetalladasDto>();

                foreach (var tutoria in tutorias)
                {
                    // Obtener tutor/profesor
                    var tutor = await _usuarioRepository.GetByIdAsync(tutoria.Usuario_idUsuario);
                    var horario = await _horarioRepository.GetByIdAsync(tutoria.Horario_idHorario);

                    // Obtener estudiantes asignados
                    var tutoriaEstudiantes = await _tutoriaEstudianteRepository.GetByTutoriaAsync(tutoria.idTutoria);
                    var estudiantes = new List<EstudianteMetricasDto>();

                    // Obtener informes de esta tutoría para calcular asistencias
                    var informes = await _informeRepository.GetByTutoriaAsync(tutoria.idTutoria);
                    int numeroAsistencias = 0;

                    foreach (var te in tutoriaEstudiantes)
                    {
                        var estudiante = await _usuarioRepository.GetByIdAsync(te.Usuario_idUsuario);
                        if (estudiante != null)
                        {
                            // Verificar si hay un informe para este estudiante específico en esta tutoría (indicaría asistencia)
                            bool asistio = informes.Any(i => i.Usuario_idUsuario == estudiante.idUsuario);
                            if (asistio)
                            {
                                numeroAsistencias++;
                            }

                            estudiantes.Add(new EstudianteMetricasDto
                            {
                                EstudianteId = estudiante.idUsuario,
                                EstudianteNombre = estudiante.nombres,
                                EstudianteApellidos = estudiante.apellidos,
                                EstudianteCorreo = estudiante.correo,
                                Asistio = asistio
                            });
                        }
                    }

                    var metrica = new MetricasTutoriasDetalladasDto
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
                        HorarioHoraFin = horario?.horaFin ?? DateTime.MinValue,
                        TutorId = tutor?.idUsuario ?? 0,
                        TutorNombre = tutor?.nombres ?? "N/A",
                        TutorApellidos = tutor?.apellidos ?? "N/A",
                        TutorCorreo = tutor?.correo ?? "N/A",
                        NumeroAsistencias = numeroAsistencias,
                        TotalEstudiantesAsignados = estudiantes.Count,
                        Estudiantes = estudiantes
                    };

                    metricas.Add(metrica);

                    // Guardar métricas en Informe si la tutoría está completada
                    if (tutoria.estado.ToLower() == "completada" && informes.Count == 0)
                    {
                        // Crear un informe consolidado para la tutoría
                        var informe = new Informe
                        {
                            descripcion = $"Informe de métricas para tutoría {tutoria.idTutoria}. Asistencias: {numeroAsistencias}/{estudiantes.Count}. Idioma: {tutoria.idioma}, Nivel: {tutoria.nivel}",
                            fechaGeneracion = DateTime.Now,
                            Tutoria_idTutoria = tutoria.idTutoria,
                            Usuario_idUsuario = tutor?.idUsuario ?? 0
                        };
                        await _informeRepository.CreateAsync(informe);
                    }
                }

                return metricas;
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Error al obtener métricas de tutorías: {ex.Message}");
            }
        }

    }
}
