using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TutoriaController : ControllerBase
    {
        private readonly ITutoriaService _tutoriaService;

        public TutoriaController(ITutoriaService tutoriaService)
        {
            _tutoriaService = tutoriaService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTutoria([FromBody] TutoriaCreateDto tutoriaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tutoria = await _tutoriaService.CreateTutoriaAsync(tutoriaDto);
                return CreatedAtAction(nameof(GetTutoria), new { id = tutoria.IdTutoria }, tutoria);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTutoria(int id)
        {
            try
            {
                var tutoria = await _tutoriaService.GetTutoriaByIdAsync(id);
                return Ok(tutoria);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTutorias()
        {
            try
            {
                var tutorias = await _tutoriaService.GetAllTutoriasAsync();
                return Ok(tutorias);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTutoria(int id, [FromBody] TutoriaCreateDto tutoriaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tutoria = await _tutoriaService.UpdateTutoriaAsync(id, tutoriaDto);
                return Ok(tutoria);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTutoria(int id)
        {
            try
            {
                var result = await _tutoriaService.DeleteTutoriaAsync(id);
                if (result)
                {
                    return Ok(new { message = "Tutoría eliminada exitosamente" });
                }
                return NotFound(new { message = "Tutoría no encontrada" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("{id}/estudiantes")]
        public async Task<IActionResult> AgregarEstudiantesATutoria(int id, [FromBody] AgregarEstudiantesTutoriaDto estudiantesDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _tutoriaService.AgregarEstudiantesATutoriaAsync(id, estudiantesDto);
                if (result)
                {
                    return Ok(new { message = "Estudiantes agregados exitosamente" });
                }
                return BadRequest(new { message = "No se pudieron agregar los estudiantes" });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("calendario/docente")]
        public async Task<IActionResult> GetCalendarioPorDocente([FromQuery] string nombre, [FromQuery] string apellidos, [FromQuery] string correo)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellidos) || string.IsNullOrEmpty(correo))
                {
                    return BadRequest(new { message = "Se requieren nombre, apellidos y correo del docente" });
                }

                var tutorias = await _tutoriaService.GetCalendarioPorDocenteAsync(nombre, apellidos, correo);
                return Ok(tutorias);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("calendario/estudiante")]
        public async Task<IActionResult> GetCalendarioPorEstudiante([FromQuery] string nombre, [FromQuery] string apellidos, [FromQuery] string correo)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellidos) || string.IsNullOrEmpty(correo))
                {
                    return BadRequest(new { message = "Se requieren nombre, apellidos y correo del estudiante" });
                }

                var tutorias = await _tutoriaService.GetCalendarioPorEstudianteAsync(nombre, apellidos, correo);
                return Ok(tutorias);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("calendario/idioma-nivel")]
        public async Task<IActionResult> GetCalendarioPorIdiomaNivel([FromQuery] string idioma, [FromQuery] string nivel)
        {
            try
            {
                if (string.IsNullOrEmpty(idioma) || string.IsNullOrEmpty(nivel))
                {
                    return BadRequest(new { message = "Se requieren idioma y nivel" });
                }

                var tutorias = await _tutoriaService.GetCalendarioPorIdiomaNivelAsync(idioma, nivel);
                return Ok(tutorias);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{id}/estudiantes")]
        public async Task<IActionResult> GetEstudiantesByTutoria(int id)
        {
            try
            {
                var estudiantes = await _tutoriaService.GetEstudiantesByTutoriaAsync(id);
                return Ok(estudiantes);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpDelete("{tutoriaId}/estudiantes/{estudianteId}")]
        public async Task<IActionResult> EliminarEstudianteDeTutoria(int tutoriaId, int estudianteId)
        {
            try
            {
                var result = await _tutoriaService.EliminarEstudianteDeTutoriaAsync(tutoriaId, estudianteId);
                if (result)
                {
                    return Ok(new { message = "Estudiante eliminado de la tutoría exitosamente" });
                }
                return NotFound(new { message = "No se pudo eliminar el estudiante de la tutoría" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("estado/{estado}")]
        public async Task<IActionResult> GetTutoriasByEstado(string estado)
        {
            try
            {
                if (string.IsNullOrEmpty(estado))
                {
                    return BadRequest(new { message = "El estado es requerido" });
                }

                var tutorias = await _tutoriaService.GetTutoriasByEstadoAsync(estado);
                return Ok(tutorias);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("estado/{estado}/usuario/{usuarioId}")]
        public async Task<IActionResult> GetTutoriasByEstadoAndUsuario(string estado, int usuarioId)
        {
            try
            {
                if (string.IsNullOrEmpty(estado))
                {
                    return BadRequest(new { message = "El estado es requerido" });
                }

                if (usuarioId <= 0)
                {
                    return BadRequest(new { message = "El ID de usuario debe ser mayor a 0" });
                }

                var tutorias = await _tutoriaService.GetTutoriasByEstadoAndUsuarioAsync(estado, usuarioId);
                return Ok(tutorias);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("{id}/finalizar")]
        public async Task<IActionResult> FinalizarTutoria(int id)
        {
            try
            {
                var tutoria = await _tutoriaService.FinalizarTutoriaAsync(id);
                return Ok(tutoria);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}
