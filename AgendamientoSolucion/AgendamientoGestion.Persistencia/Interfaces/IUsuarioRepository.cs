using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface IUsuarioRepository {
    Task<Usuario> CreateAsync(Usuario usuario);
    Task<Usuario> GetByIdAsync(int id);
    Task<Usuario> GetByEmailAsync(string email);
    Task<Usuario> GetByNombreApellidosCorreoAsync(string nombres, string apellidos, string correo);
    Task<List<Usuario>> GetAllAsync();
    Task<Usuario> UpdateAsync(Usuario usuario);
    Task<bool> DeleteAsync(int id);
    Task<List<Usuario>> GetByRolAsync(string tipoRol);
    /// <summary>Estado de la cuenta sin cargar la entidad: null si el usuario no existe.</summary>
    Task<bool?> GetActivoAsync(int id);
    /// <summary>activo y credencialesCambiadasEn sin cargar la entidad; null si el usuario no existe.</summary>
    Task<(bool Activo, DateTime? CredencialesCambiadasEn)?> GetEstadoCredencialesAsync(int id);
    Task<int> ContarAdministradoresActivosAsync();

    /// <summary>
    /// Elimina definitivamente al usuario resolviendo todas sus dependencias (ver docs/api, "Eliminación de usuarios").
    /// Debe llamarse dentro de una transacción. null si el usuario no existe.
    /// </summary>
    Task<ResultadoEliminacionUsuario?> EliminarConDependenciasAsync(int id);

    /// <summary>
    /// Página de usuarios filtrada y su total (antes de paginar). Cada término de <paramref name="terminos"/> debe
    /// aparecer en nombres, apellidos o correo (sin distinguir mayúsculas ni tildes). Orden: apellidos, nombres, idUsuario.
    /// </summary>
    Task<(List<Usuario> Usuarios, int Total)> BuscarPaginadoAsync(IReadOnlyList<string> terminos, string? tipoRol, bool? activo, int skip, int take);
}
