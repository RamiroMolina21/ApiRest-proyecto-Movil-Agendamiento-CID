using AgendamientoGestion.Persistencia.Interfaces;
using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces;

public interface IUsuarioService {
    /// <param name="rolSolicitante">Rol del usuario autenticado. Si no es Administrador solo puede gestionar Estudiantes.</param>
    Task<UsuarioResponseDto> CreateUsuarioAsync(UsuarioCreateDto usuarioDto, string rolSolicitante);
    Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id);
    Task<List<UsuarioResponseDto>> GetAllUsuariosAsync();
    /// <summary>Búsqueda paginada (GET /api/Usuario con parámetros). 400 si page/pageSize/rol/q no son válidos.</summary>
    Task<PaginaDto<UsuarioResponseDto>> BuscarUsuariosAsync(UsuarioBusquedaDto busqueda);
    Task<UsuarioResponseDto> UpdateUsuarioAsync(int id, UsuarioUpdateDto usuarioDto, string rolSolicitante);
    /// <summary>Eliminación definitiva con dependencias resueltas. 400 sí mismo; 403 Auxiliar sobre no Estudiante; 404; 409 último Administrador activo.</summary>
    Task<ResultadoEliminacionUsuario> DeleteUsuarioAsync(int id, int solicitanteId, string rolSolicitante);
    /// <param name="incluirInactivos">false (por defecto): solo cuentas activas (listas para asignar).</param>
    Task<List<UsuarioResponseDto>> GetUsuariosByRolAsync(string tipoRol, bool incluirInactivos = false);

    /// <summary>
    /// Activa o desactiva una cuenta. Administrador: cualquiera; Auxiliar: solo Estudiantes (403).
    /// 409 si alguien intenta desactivarse a sí mismo o desactivar al último Administrador activo.
    /// </summary>
    Task<UsuarioResponseDto> CambiarEstadoAsync(int id, bool activo, int solicitanteId, string rolSolicitante);
}
