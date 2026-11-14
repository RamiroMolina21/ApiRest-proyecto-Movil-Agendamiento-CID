using Agendamiento.Security;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Agendamiento.Controllers
{
    /// <summary>
    /// Regla de propiedad (Docente dueño del horario o asignado; Tutor solo asignado; Admin/Aux todo):
    /// ver AgendamientoGestion.Logica.Seguridad.ReglasAccesoTutoria y docs/api/autenticacion-y-autorizacion.md.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TutoriaController : ControllerBase
    {
        private readonly ITutoriaService _tutoriaService;

        public TutoriaController(ITutoriaService tutoriaService)
        {
            _tutoriaService = tutoriaService;
        }

        /// <summary>Admin/Aux en cualquier horario; Docente solo en sus horarios; Tutor 403.</summary>
        [HttpPost]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> CreateTutoria([FromBody] TutoriaCreateDto tutoriaDto)
        {
            var tutoria = await _tutoriaService.CreateTutoriaAsync(tutoriaDto, User.ComoSolicitante());
            return CreatedAtAction(nameof(GetTutoria), new { id = tutoria.IdTutoria }, tutoria);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetTutoria(int id)
        {
            return Ok(await _tutoriaService.GetTutoriaByIdAsync(id, User.ComoSolicitante()));
        }

        [HttpGet]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetAllTutorias()
        {
            return Ok(await _tutoriaService.GetAllTutoriasAsync());
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> UpdateTutoria(int id, [FromBody] TutoriaCreateDto tutoriaDto)
        {
            return Ok(await _tutoriaService.UpdateTutoriaAsync(id, tutoriaDto));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> DeleteTutoria(int id)
        {
            await _tutoriaService.DeleteTutoriaAsync(id);
            return Ok(new { message = "Tutoría eliminada exitosamente" });
        }

        [HttpPost("{id}/estudiantes")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> AgregarEstudiantesATutoria(int id, [FromBody] AgregarEstudiantesTutoriaDto estudiantesDto)
        {
            if (!await _tutoriaService.AgregarEstudiantesATutoriaAsync(id, estudiantesDto, User.ComoSolicitante()))
            {
                throw new ValidationException("No se pudieron agregar los estudiantes (ya estaban asignados a la tutoría)");
            }

            return Ok(new { message = "Estudiantes agregados exitosamente" });
        }

        [HttpGet("calendario/docente")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetCalendarioPorDocente([FromQuery] string? nombre, [FromQuery] string? apellidos, [FromQuery] string? correo)
        {
            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellidos) || string.IsNullOrEmpty(correo))
            {
                throw new ValidationException("Se requieren nombre, apellidos y correo del docente");
            }

            return Ok(await _tutoriaService.GetCalendarioPorDocenteAsync(nombre, apellidos, correo));
        }

        [HttpGet("calendario/estudiante")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetCalendarioPorEstudiante([FromQuery] string? nombre, [FromQuery] string? apellidos, [FromQuery] string? correo)
        {
            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellidos) || string.IsNullOrEmpty(correo))
            {
                throw new ValidationException("Se requieren nombre, apellidos y correo del estudiante");
            }

            return Ok(await _tutoriaService.GetCalendarioPorEstudianteAsync(nombre, apellidos, correo));
        }

        [HttpGet("calendario/idioma-nivel")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetCalendarioPorIdiomaNivel([FromQuery] string? idioma, [FromQuery] string? nivel)
        {
            if (string.IsNullOrEmpty(idioma) || string.IsNullOrEmpty(nivel))
            {
                throw new ValidationException("Se requieren idioma y nivel");
            }

            return Ok(await _tutoriaService.GetCalendarioPorIdiomaNivelAsync(idioma, nivel));
        }

        [HttpGet("{id}/estudiantes")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetEstudiantesByTutoria(int id)
        {
            return Ok(await _tutoriaService.GetEstudiantesByTutoriaAsync(id, User.ComoSolicitante()));
        }

        /// <summary>Antes solo GestionWeb; ahora también el Docente participante (el Tutor recibe 403).</summary>
        [HttpDelete("{tutoriaId}/estudiantes/{estudianteId}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> EliminarEstudianteDeTutoria(int tutoriaId, int estudianteId)
        {
            if (!await _tutoriaService.EliminarEstudianteDeTutoriaAsync(tutoriaId, estudianteId, User.ComoSolicitante()))
            {
                throw new NotFoundException("No se pudo eliminar el estudiante de la tutoría");
            }

            return Ok(new { message = "Estudiante eliminado de la tutoría exitosamente" });
        }

        /// <summary>Filtro exacto por el valor guardado (no aplica sinónimos; ver EstadosTutoria).</summary>
        [HttpGet("estado/{estado}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetTutoriasByEstado(string estado)
        {
            return Ok(await _tutoriaService.GetTutoriasByEstadoAsync(estado));
        }

        /// <summary>
        /// Tutorías ASIGNADAS al usuario. El estado es un grupo de sinónimos sin distinguir mayúsculas
        /// (pendiente = Pendiente/Programada/Activo; finalizada = Finalizada/Completada).
        /// Docente/Tutor solo pueden pedir su propio usuarioId.
        /// </summary>
        [HttpGet("estado/{estado}/usuario/{usuarioId}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetTutoriasByEstadoAndUsuario(string estado, int usuarioId)
        {
            ValidarUsuarioId(usuarioId);
            User.AsegurarAccesoADatosDe(usuarioId);
            return Ok(await _tutoriaService.GetTutoriasByEstadoAndUsuarioAsync(estado, usuarioId));
        }

        /// <summary>
        /// Tutorías AGENDADAS en horarios del usuario (dueño del horario), aunque estén asignadas a otro.
        /// <c>estado</c> opcional (mismos grupos de sinónimos). Docente/Tutor solo su propio usuarioId.
        /// </summary>
        [HttpGet("agendadas/usuario/{usuarioId}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetTutoriasAgendadas(int usuarioId, [FromQuery] string? estado)
        {
            ValidarUsuarioId(usuarioId);
            User.AsegurarAccesoADatosDe(usuarioId);
            return Ok(await _tutoriaService.GetTutoriasAgendadasPorUsuarioAsync(usuarioId, estado));
        }

        /// <summary>
        /// Marca la tutoría como "Finalizada" y envía las encuestas (idempotente). Cuerpo OPCIONAL
        /// <c>{ calificacion, comentario }</c>: crea la retroalimentación del usuario del token en la misma transacción.
        /// </summary>
        [HttpPut("{id}/finalizar")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> FinalizarTutoria(
            int id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] FinalizarTutoriaDto? datos)
        {
            return Ok(await _tutoriaService.FinalizarTutoriaAsync(id, datos, User.ComoSolicitante()));
        }

        private static void ValidarUsuarioId(int usuarioId)
        {
            if (usuarioId <= 0)
            {
                throw new ValidationException("El ID de usuario debe ser mayor a 0");
            }
        }
    }
}
