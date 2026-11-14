using Agendamiento.Security;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Controllers
{
    /// <summary>
    /// Escritura: Administrador (cualquier usuario) y Auxiliar (solo usuarios con rol Estudiante; regla en UsuarioService).
    /// Lectura: Administrador/Auxiliar; by-rol también Docente/Tutor (lo usa MAUI).
    /// Nota: las políticas de clase y de acción se combinan con AND; por eso cada acción declara la suya.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpPost]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> CreateUsuario([FromBody] UsuarioCreateDto usuarioDto)
        {
            var usuario = await _usuarioService.CreateUsuarioAsync(usuarioDto, User.GetRol());
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.IdUsuario }, usuario);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetUsuario(int id)
        {
            return Ok(await _usuarioService.GetUsuarioByIdAsync(id));
        }

        /// <summary>
        /// Sin parámetros: array completo (comportamiento original). Con cualquiera de page, pageSize, q, rol o
        /// activo: una página (orden apellidos, nombres, idUsuario) y el total en la cabecera X-Total-Count.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> GetAllUsuarios([FromQuery] UsuarioBusquedaDto busqueda)
        {
            if (!busqueda.HayParametros)
            {
                return Ok(await _usuarioService.GetAllUsuariosAsync());
            }

            var pagina = await _usuarioService.BuscarUsuariosAsync(busqueda);
            Response.Headers["X-Total-Count"] = pagina.Total.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Ok(pagina.Items);
        }

        /// <summary>Por defecto solo cuentas activas; ?incluirInactivos=true devuelve también las desactivadas.</summary>
        [HttpGet("by-rol/{tipoRol}")]
        [Authorize(Policy = Politicas.Personal)]
        public async Task<IActionResult> GetUsuariosByRol(string tipoRol, [FromQuery] bool incluirInactivos = false)
        {
            return Ok(await _usuarioService.GetUsuariosByRolAsync(tipoRol, incluirInactivos));
        }

        /// <summary>
        /// Habilita o deshabilita una cuenta. Administrador: cualquiera; Auxiliar: solo Estudiantes (403).
        /// 409 al desactivarse a sí mismo o al último Administrador activo.
        /// </summary>
        [HttpPatch("{id}/estado")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] UsuarioEstadoDto dto)
        {
            var solicitanteId = User.GetUsuarioId() ?? throw new UnauthorizedException("Token sin identificador de usuario");
            return Ok(await _usuarioService.CambiarEstadoAsync(id, dto.Activo!.Value, solicitanteId, User.GetRol()));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] UsuarioUpdateDto usuarioDto)
        {
            return Ok(await _usuarioService.UpdateUsuarioAsync(id, usuarioDto, User.GetRol()));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Politicas.GestionWeb)]
        /// <summary>
        /// Eliminación definitiva. Ya no se bloquea por tutorías/horarios/retroalimentaciones: se borran los datos
        /// propios y se conservan desvinculados los compartidos (ver docs/api/endpoints.md, "Eliminación de usuarios").
        /// </summary>
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var solicitanteId = User.GetUsuarioId() ?? throw new UnauthorizedException("Token sin identificador de usuario");
            var resumen = await _usuarioService.DeleteUsuarioAsync(id, solicitanteId, User.GetRol());
            return Ok(new { message = "Usuario eliminado exitosamente", resumen });
        }
    }
}
