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
}
