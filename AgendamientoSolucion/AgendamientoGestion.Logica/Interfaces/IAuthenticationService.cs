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
    }
}
