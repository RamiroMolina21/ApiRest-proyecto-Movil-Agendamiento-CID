using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces
{
    public interface IAuthenticationService
    {
        Task<UsuarioResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> ValidatePasswordAsync(string password, string hashedPassword);

        /// <summary>
        /// Cambia la contraseña del usuario <paramref name="usuarioId"/> (el "sub" del token, nunca otro).
        /// 400 si la actual no coincide o la nueva no cumple la política; 401 si el usuario ya no existe.
        /// </summary>
        Task CambiarContrasenaAsync(int usuarioId, CambiarContrasenaDto dto);
    }
}
