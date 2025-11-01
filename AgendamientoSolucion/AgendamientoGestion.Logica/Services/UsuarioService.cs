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

namespace AgendamientoGestion.Logica.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository, IRolRepository rolRepository)
    {
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
    }

    public async Task<UsuarioResponseDto> CreateUsuarioAsync(UsuarioCreateDto usuarioDto)
    {
        // Validar formato de correo
        EmailValidator.ValidateEmail(usuarioDto.Correo, "Correo");

        // Verificar que el correo no exista
        var usuarioExistente = await _usuarioRepository.GetByEmailAsync(usuarioDto.Correo);
        if (usuarioExistente != null)
        {
            throw new BusinessException("Ya existe un usuario con este correo");
        }

        // Verificar que el rol existe
        var rol = await _rolRepository.GetByIdAsync(usuarioDto.RolId);
        if (rol == null)
        {
            throw new NotFoundException("El rol especificado no existe");
        }

        // Encriptar contraseña
        var contrasenaHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Contrasena);

        // Crear usuario
        var usuario = new Usuario
        {
            nombres = usuarioDto.Nombres,
            apellidos = usuarioDto.Apellidos,
            correo = usuarioDto.Correo,
            contrasenaHash = contrasenaHash,
            fechaRegistro = DateTime.Now,
            Rol_idRol = usuarioDto.RolId
        };

        var usuarioCreado = await _usuarioRepository.CreateAsync(usuario);

        return new UsuarioResponseDto
        {
            IdUsuario = usuarioCreado.idUsuario,
            Nombres = usuarioCreado.nombres,
            Apellidos = usuarioCreado.apellidos,
            Correo = usuarioCreado.correo,
            FechaRegistro = usuarioCreado.fechaRegistro,
            TipoRol = rol.tipoRol
        };
    }

    public async Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        var rol = await _rolRepository.GetByIdAsync(usuario.Rol_idRol);

        return new UsuarioResponseDto
        {
            IdUsuario = usuario.idUsuario,
            Nombres = usuario.nombres,
            Apellidos = usuario.apellidos,
            Correo = usuario.correo,
            FechaRegistro = usuario.fechaRegistro,
            TipoRol = rol?.tipoRol ?? "Sin rol"
        };
    }

    public async Task<List<UsuarioResponseDto>> GetAllUsuariosAsync()
    {
        var usuarios = await _usuarioRepository.GetAllAsync();
        var usuariosDto = new List<UsuarioResponseDto>();

        foreach (var usuario in usuarios)
        {
            var rol = await _rolRepository.GetByIdAsync(usuario.Rol_idRol);
            usuariosDto.Add(new UsuarioResponseDto
            {
                IdUsuario = usuario.idUsuario,
                Nombres = usuario.nombres,
                Apellidos = usuario.apellidos,
                Correo = usuario.correo,
                FechaRegistro = usuario.fechaRegistro,
                TipoRol = rol?.tipoRol ?? "Sin rol"
            });
        }

        return usuariosDto;
    }

    public async Task<UsuarioResponseDto> UpdateUsuarioAsync(int id, UsuarioCreateDto usuarioDto)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Validar formato de correo
        EmailValidator.ValidateEmail(usuarioDto.Correo, "Correo");

        // Verificar que el rol existe
        var rol = await _rolRepository.GetByIdAsync(usuarioDto.RolId);
        if (rol == null)
        {
            throw new NotFoundException("El rol especificado no existe");
        }

        // Actualizar datos
        usuario.nombres = usuarioDto.Nombres;
        usuario.apellidos = usuarioDto.Apellidos;
        usuario.correo = usuarioDto.Correo;
        usuario.Rol_idRol = usuarioDto.RolId;

        // Solo actualizar contraseña si se proporciona una nueva
        if (!string.IsNullOrEmpty(usuarioDto.Contrasena))
        {
            usuario.contrasenaHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Contrasena);
        }

        await _usuarioRepository.UpdateAsync(usuario);

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

    public async Task<bool> DeleteUsuarioAsync(int id)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        return await _usuarioRepository.DeleteAsync(id);
    }

    public async Task<List<UsuarioResponseDto>> GetUsuariosByRolAsync(string tipoRol)
    {
        var usuarios = await _usuarioRepository.GetByRolAsync(tipoRol);
        var usuariosDto = new List<UsuarioResponseDto>();

        foreach (var usuario in usuarios)
        {
            var rol = await _rolRepository.GetByIdAsync(usuario.Rol_idRol);
            usuariosDto.Add(new UsuarioResponseDto
            {
                IdUsuario = usuario.idUsuario,
                Nombres = usuario.nombres,
                Apellidos = usuario.apellidos,
                Correo = usuario.correo,
                FechaRegistro = usuario.fechaRegistro,
                TipoRol = rol?.tipoRol ?? "Sin rol"
            });
        }

        return usuariosDto;
    }
}
