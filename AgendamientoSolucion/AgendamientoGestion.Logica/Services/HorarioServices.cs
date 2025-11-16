using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Services;

public class HorarioServices : IHorarioServices
{
    private readonly IHorarioRepository _horarioRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public HorarioServices(IHorarioRepository horarioRepository, IUsuarioRepository usuarioRepository)
    {
        _horarioRepository = horarioRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<HorarioResponseDto> CreateHorarioAsync(HorarioCreateDto horarioDto)
    {
        // Buscar usuario por ID
        var usuario = await _usuarioRepository.GetByIdAsync(horarioDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Crear horario
        var horario = new Horario
        {
            titulo = horarioDto.Titulo,
            fechaInicio = horarioDto.FechaInicio,
            fechaFin = horarioDto.FechaFin,
            horaInicio = horarioDto.HoraInicio,
            horaFin = horarioDto.HoraFin,
            cupos = horarioDto.Cupos,
            espacio = horarioDto.Espacio,
            estado = horarioDto.Estado,
            Usuario_idUsuario = usuario.idUsuario
        };

        var horarioCreado = await _horarioRepository.CreateAsync(horario);

        return new HorarioResponseDto
        {
            IdHorario = horarioCreado.idHorario,
            Titulo = horarioCreado.titulo,
            FechaInicio = horarioCreado.fechaInicio,
            FechaFin = horarioCreado.fechaFin,
            HoraInicio = horarioCreado.horaInicio,
            HoraFin = horarioCreado.horaFin,
            Cupos = horarioCreado.cupos,
            Espacio = horarioCreado.espacio,
            Estado = horarioCreado.estado,
            UsuarioNombre = usuario.nombres,
            UsuarioApellidos = usuario.apellidos,
            UsuarioCorreo = usuario.correo
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
            Titulo = horario.titulo,
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
                Titulo = horario.titulo,
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

        // Buscar usuario por ID
        var usuario = await _usuarioRepository.GetByIdAsync(horarioDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Actualizar datos
        horario.titulo = horarioDto.Titulo;
        horario.fechaInicio = horarioDto.FechaInicio;
        horario.fechaFin = horarioDto.FechaFin;
        horario.horaInicio = horarioDto.HoraInicio;
        horario.horaFin = horarioDto.HoraFin;
        horario.cupos = horarioDto.Cupos;
        horario.espacio = horarioDto.Espacio;
        horario.estado = horarioDto.Estado;
        horario.Usuario_idUsuario = usuario.idUsuario;

        await _horarioRepository.UpdateAsync(horario);

        return new HorarioResponseDto
        {
            IdHorario = horario.idHorario,
            Titulo = horario.titulo,
            FechaInicio = horario.fechaInicio,
            FechaFin = horario.fechaFin,
            HoraInicio = horario.horaInicio,
            HoraFin = horario.horaFin,
            Cupos = horario.cupos,
            Espacio = horario.espacio,
            Estado = horario.estado,
            UsuarioNombre = usuario.nombres,
            UsuarioApellidos = usuario.apellidos,
            UsuarioCorreo = usuario.correo
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
                Titulo = horario.titulo,
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
                Titulo = horario.titulo,
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
