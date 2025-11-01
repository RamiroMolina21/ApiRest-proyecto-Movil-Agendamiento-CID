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

public class HorarioServices : IHorarioServices
{
    private readonly IHorarioRepository _horarioRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;

    public HorarioServices(IHorarioRepository horarioRepository, IUsuarioRepository usuarioRepository, IRolRepository rolRepository)
    {
        _horarioRepository = horarioRepository;
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
    }

    public async Task<HorarioResponseDto> CreateHorarioAsync(HorarioCreateDto horarioDto)
    {
        // Validar formato de correo
        EmailValidator.ValidateEmail(horarioDto.DocenteCorreo, "Correo del docente");

        // Buscar docente por nombre, apellidos y correo
        var docente = await _usuarioRepository.GetByNombreApellidosCorreoAsync(
            horarioDto.DocenteNombre, 
            horarioDto.DocenteApellidos, 
            horarioDto.DocenteCorreo);

        // Si no existe, buscar por correo para verificar si hay inconsistencia
        if (docente == null)
        {
            var docentePorCorreo = await _usuarioRepository.GetByEmailAsync(horarioDto.DocenteCorreo);
            if (docentePorCorreo != null)
            {
                throw new ValidationException("Ya existe un usuario con este correo pero con datos diferentes");
            }

            // Buscar rol docente
            var rolDocente = await _rolRepository.GetByTipoAsync("Docente");
            if (rolDocente == null)
            {
                throw new NotFoundException("El rol 'Docente' no existe en el sistema");
            }

            // Crear nuevo docente
            docente = new Usuario
            {
                nombres = horarioDto.DocenteNombre,
                apellidos = horarioDto.DocenteApellidos,
                correo = horarioDto.DocenteCorreo,
                contrasenaHash = BCrypt.Net.BCrypt.HashPassword("Temporal123!"), // Contraseña temporal
                fechaRegistro = DateTime.Now,
                Rol_idRol = rolDocente.idRol
            };

            docente = await _usuarioRepository.CreateAsync(docente);
        }
        else
        {
            // Verificar que el usuario tenga rol de docente
            if (docente.Rol == null || docente.Rol.tipoRol.ToLower() != "docente")
            {
                throw new ValidationException("El usuario especificado no tiene rol de docente");
            }
        }

        // Crear horario
        var horario = new Horario
        {
            fechaInicio = horarioDto.FechaInicio,
            fechaFin = horarioDto.FechaFin,
            horaInicio = horarioDto.HoraInicio,
            horaFin = horarioDto.HoraFin,
            cupos = horarioDto.Cupos,
            espacio = horarioDto.Espacio,
            estado = horarioDto.Estado,
            Usuario_idUsuario = docente.idUsuario
        };

        var horarioCreado = await _horarioRepository.CreateAsync(horario);

        return new HorarioResponseDto
        {
            IdHorario = horarioCreado.idHorario,
            FechaInicio = horarioCreado.fechaInicio,
            FechaFin = horarioCreado.fechaFin,
            HoraInicio = horarioCreado.horaInicio,
            HoraFin = horarioCreado.horaFin,
            Cupos = horarioCreado.cupos,
            Espacio = horarioCreado.espacio,
            Estado = horarioCreado.estado,
            UsuarioNombre = docente.nombres,
            UsuarioApellidos = docente.apellidos,
            UsuarioCorreo = docente.correo
        };
    }

    public async Task<HorarioResponseDto> GetHorarioByIdAsync(int id)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(horario.Usuario_idUsuario);

        return new HorarioResponseDto
        {
            IdHorario = horario.idHorario,
            FechaInicio = horario.fechaInicio,
            FechaFin = horario.fechaFin,
            HoraInicio = horario.horaInicio,
            HoraFin = horario.horaFin,
            Cupos = horario.cupos,
            Espacio = horario.espacio,
            Estado = horario.estado,
            UsuarioNombre = usuario?.nombres ?? "N/A",
            UsuarioApellidos = usuario?.apellidos ?? "N/A",
            UsuarioCorreo = usuario?.correo ?? "N/A"
        };
    }

    public async Task<List<HorarioResponseDto>> GetAllHorariosAsync()
    {
        var horarios = await _horarioRepository.GetAllAsync();
        var horariosDto = new List<HorarioResponseDto>();

        foreach (var horario in horarios)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(horario.Usuario_idUsuario);
            horariosDto.Add(new HorarioResponseDto
            {
                IdHorario = horario.idHorario,
                FechaInicio = horario.fechaInicio,
                FechaFin = horario.fechaFin,
                HoraInicio = horario.horaInicio,
                HoraFin = horario.horaFin,
                Cupos = horario.cupos,
                Espacio = horario.espacio,
                Estado = horario.estado,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A"
            });
        }

        return horariosDto;
    }

    public async Task<HorarioResponseDto> UpdateHorarioAsync(int id, HorarioCreateDto horarioDto)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        // Validar formato de correo
        EmailValidator.ValidateEmail(horarioDto.DocenteCorreo, "Correo del docente");

        // Buscar docente por nombre, apellidos y correo
        var docente = await _usuarioRepository.GetByNombreApellidosCorreoAsync(
            horarioDto.DocenteNombre, 
            horarioDto.DocenteApellidos, 
            horarioDto.DocenteCorreo);

        // Si no existe, buscar por correo para verificar si hay inconsistencia
        if (docente == null)
        {
            var docentePorCorreo = await _usuarioRepository.GetByEmailAsync(horarioDto.DocenteCorreo);
            if (docentePorCorreo != null)
            {
                throw new ValidationException("Ya existe un usuario con este correo pero con datos diferentes");
            }

            // Buscar rol docente
            var rolDocente = await _rolRepository.GetByTipoAsync("Docente");
            if (rolDocente == null)
            {
                throw new NotFoundException("El rol 'Docente' no existe en el sistema");
            }

            // Crear nuevo docente
            docente = new Usuario
            {
                nombres = horarioDto.DocenteNombre,
                apellidos = horarioDto.DocenteApellidos,
                correo = horarioDto.DocenteCorreo,
                contrasenaHash = BCrypt.Net.BCrypt.HashPassword("Temporal123!"), // Contraseña temporal
                fechaRegistro = DateTime.Now,
                Rol_idRol = rolDocente.idRol
            };

            docente = await _usuarioRepository.CreateAsync(docente);
        }
        else
        {
            // Verificar que el usuario tenga rol de docente
            if (docente.Rol == null || docente.Rol.tipoRol.ToLower() != "docente")
            {
                throw new ValidationException("El usuario especificado no tiene rol de docente");
            }
        }

        // Actualizar datos
        horario.fechaInicio = horarioDto.FechaInicio;
        horario.fechaFin = horarioDto.FechaFin;
        horario.horaInicio = horarioDto.HoraInicio;
        horario.horaFin = horarioDto.HoraFin;
        horario.cupos = horarioDto.Cupos;
        horario.espacio = horarioDto.Espacio;
        horario.estado = horarioDto.Estado;
        horario.Usuario_idUsuario = docente.idUsuario;

        await _horarioRepository.UpdateAsync(horario);

        return new HorarioResponseDto
        {
            IdHorario = horario.idHorario,
            FechaInicio = horario.fechaInicio,
            FechaFin = horario.fechaFin,
            HoraInicio = horario.horaInicio,
            HoraFin = horario.horaFin,
            Cupos = horario.cupos,
            Espacio = horario.espacio,
            Estado = horario.estado,
            UsuarioNombre = docente.nombres,
            UsuarioApellidos = docente.apellidos,
            UsuarioCorreo = docente.correo
        };
    }

    public async Task<bool> DeleteHorarioAsync(int id)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        return await _horarioRepository.DeleteAsync(id);
    }

    public async Task<List<HorarioResponseDto>> GetHorariosByUsuarioAsync(int usuarioId)
    {
        var horarios = await _horarioRepository.GetByUsuarioAsync(usuarioId);
        var horariosDto = new List<HorarioResponseDto>();

        foreach (var horario in horarios)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(horario.Usuario_idUsuario);
            horariosDto.Add(new HorarioResponseDto
            {
                IdHorario = horario.idHorario,
                FechaInicio = horario.fechaInicio,
                FechaFin = horario.fechaFin,
                HoraInicio = horario.horaInicio,
                HoraFin = horario.horaFin,
                Cupos = horario.cupos,
                Espacio = horario.espacio,
                Estado = horario.estado,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A"
            });
        }

        return horariosDto;
    }

    public async Task<List<HorarioResponseDto>> GetHorariosDisponiblesAsync()
    {
        var horarios = await _horarioRepository.GetDisponiblesAsync();
        var horariosDto = new List<HorarioResponseDto>();

        foreach (var horario in horarios)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(horario.Usuario_idUsuario);
            horariosDto.Add(new HorarioResponseDto
            {
                IdHorario = horario.idHorario,
                FechaInicio = horario.fechaInicio,
                FechaFin = horario.fechaFin,
                HoraInicio = horario.horaInicio,
                HoraFin = horario.horaFin,
                Cupos = horario.cupos,
                Espacio = horario.espacio,
                Estado = horario.estado,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A"
            });
        }

        return horariosDto;
    }
}
