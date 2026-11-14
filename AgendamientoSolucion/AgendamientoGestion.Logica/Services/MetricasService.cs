using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Entidades.Constants;
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
        private readonly ITutoriaEstudianteRepository _tutoriaEstudianteRepository;
        private readonly IInformeRepository _informeRepository;
        private readonly IMetricasRepository _metricasRepository;

        public MetricasService(
            ITutoriaRepository tutoriaRepository,
            ITutoriaEstudianteRepository tutoriaEstudianteRepository,
            IInformeRepository informeRepository,
            IMetricasRepository metricasRepository)
        {
            _tutoriaRepository = tutoriaRepository;
            _tutoriaEstudianteRepository = tutoriaEstudianteRepository;
            _informeRepository = informeRepository;
            _metricasRepository = metricasRepository;
        }

        /// <summary>
        /// Conteos agrupados en SQL (4 consultas pequeñas) en lugar de cargar las 4 tablas enteras. Las reglas de
        /// sinónimos (EstadosTutoria/EstadosHorario/Roles) se aplican sobre los grupos: mismo resultado que antes.
        /// </summary>
        public async Task<MetricasResponseDto> GetMetricasGeneralesAsync()
        {
            var tutorias = await _metricasRepository.ContarTutoriasPorEstadoAsync();
            var usuarios = await _metricasRepository.ContarUsuariosPorRolAsync();
            var horarios = await _metricasRepository.ContarHorariosPorEstadoAsync();
            var (suma, cantidad) = await _metricasRepository.SumarCalificacionesAsync();

            return new MetricasResponseDto
            {
                TotalTutorias = tutorias.Sum(t => t.Cantidad),
                // Tolerante a sinónimos (ver EstadosTutoria): "Finalizada"/"Completada" y "Pendiente"/"Programada"/"Activo".
                TutoriasCompletadas = tutorias.Where(t => EstadosTutoria.EsFinalizada(t.Estado)).Sum(t => t.Cantidad),
                TutoriasCanceladas = tutorias.Where(t => EstadosTutoria.EsCancelada(t.Estado)).Sum(t => t.Cantidad),
                TutoriasProgramadas = tutorias.Where(t => EstadosTutoria.EsPendiente(t.Estado)).Sum(t => t.Cantidad),
                // Igual que Enumerable.Average(decimal): suma decimal / número de elementos.
                PromedioCalificacion = cantidad > 0 ? suma / cantidad : 0,
                TotalUsuarios = usuarios.Sum(u => u.Cantidad),
                TotalDocentes = usuarios.Where(u => Roles.Es(u.TipoRol, Roles.Docente)).Sum(u => u.Cantidad),
                TotalEstudiantes = usuarios.Where(u => Roles.Es(u.TipoRol, Roles.Estudiante)).Sum(u => u.Cantidad),
                TotalHorarios = horarios.Sum(h => h.Cantidad),
                HorariosDisponibles = horarios.Where(h => EstadosHorario.EsDisponible(h.Estado)).Sum(h => h.Cantidad)
            };
        }

        /// <summary>
        /// Solo lectura. 3 consultas en total (tutorías con asignado y horario, inscripciones con usuario, informes)
        /// en lugar de 4 + 1 por estudiante por cada tutoría.
        /// </summary>
        public async Task<List<MetricasTutoriasDetalladasDto>> GetMetricasTutoriasAsync()
        {
            var tutorias = await _tutoriaRepository.GetAllAsync();
            var ids = tutorias.Select(t => t.idTutoria).ToList();
            var inscripciones = (await _tutoriaEstudianteRepository.GetByTutoriasAsync(ids))
                .GroupBy(te => te.Tutoria_idTutoria).ToDictionary(g => g.Key, g => g.ToList());
            var informes = (await _informeRepository.GetByTutoriasAsync(ids))
                .GroupBy(i => i.Tutoria_idTutoria).ToDictionary(g => g.Key, g => g.ToList());

            var metricas = new List<MetricasTutoriasDetalladasDto>();
            foreach (var tutoria in tutorias)
            {
                var tutor = tutoria.Usuario;
                var horario = tutoria.Horario;
                var informesTutoria = informes.TryGetValue(tutoria.idTutoria, out var inf) ? inf : new List<Informe>();
                var estudiantes = new List<EstudianteMetricasDto>();
                var numeroAsistencias = 0;

                foreach (var te in inscripciones.TryGetValue(tutoria.idTutoria, out var lista) ? lista : new List<TutoriaEstudiante>())
                {
                    var estudiante = te.Usuario;
                    if (estudiante == null) continue;

                    // Un informe de este estudiante en esta tutoría indica asistencia.
                    var asistio = informesTutoria.Any(i => i.Usuario_idUsuario == estudiante.idUsuario);
                    if (asistio) numeroAsistencias++;

                    estudiantes.Add(new EstudianteMetricasDto
                    {
                        EstudianteId = estudiante.idUsuario,
                        EstudianteNombre = estudiante.nombres,
                        EstudianteApellidos = estudiante.apellidos,
                        EstudianteCorreo = estudiante.correo,
                        Asistio = asistio
                    });
                }

                metricas.Add(new MetricasTutoriasDetalladasDto
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
                });
            }

            return metricas;
        }
    }
}