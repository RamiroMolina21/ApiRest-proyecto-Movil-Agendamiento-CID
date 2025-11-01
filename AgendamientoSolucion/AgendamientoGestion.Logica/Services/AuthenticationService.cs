using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRolRepository _rolRepository;

        public AuthenticationService(IUsuarioRepository usuarioRepository, IRolRepository rolRepository)
        {
            _usuarioRepository = usuarioRepository;
            _rolRepository = rolRepository;
        }

        public async Task<UsuarioResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Validar formato de correo
            EmailValidator.ValidateEmail(loginDto.Correo, "Correo");

            // Buscar usuario por correo
            var usuario = await _usuarioRepository.GetByEmailAsync(loginDto.Correo);
            if (usuario == null)
            {
                throw new NotFoundException("Usuario no encontrado");
            }

            // Verificar contraseña
            if (!ValidatePasswordAsync(loginDto.Contrasena, usuario.contrasenaHash).Result)
            {
                throw new UnauthorizedException("Credenciales inválidas");
            }

            // Verificar que el usuario tenga acceso a la aplicación
            var rol = await _rolRepository.GetByIdAsync(usuario.Rol_idRol);
            if (rol == null || (rol.tipoRol != "Administrador" && rol.tipoRol != "Docente" && rol.tipoRol != "Tutor" && rol.tipoRol != "Auxiliar"))
            {
                throw new UnauthorizedException("El usuario no tiene acceso a la aplicación");
            }

            return new UsuarioResponseDto
            {
                IdUsuario = usuario.idUsuario,
                Nombres = usuario.nombres,
                Apellidos = usuario.apellidos,
                Correo = usuario.correo,
                FechaRegistro = usuario.fechaRegistro,
                TipoRol = rol.tipoRol
            };
        }


        public async Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
