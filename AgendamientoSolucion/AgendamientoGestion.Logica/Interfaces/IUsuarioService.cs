using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces;

public interface IUsuarioService {
    Task<UsuarioResponseDto> CreateUsuarioAsync(UsuarioCreateDto usuarioDto);
    Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id);
    Task<List<UsuarioResponseDto>> GetAllUsuariosAsync();
    Task<UsuarioResponseDto> UpdateUsuarioAsync(int id, UsuarioCreateDto usuarioDto);
    Task<bool> DeleteUsuarioAsync(int id);
    Task<List<UsuarioResponseDto>> GetUsuariosByRolAsync(string tipoRol);
}
