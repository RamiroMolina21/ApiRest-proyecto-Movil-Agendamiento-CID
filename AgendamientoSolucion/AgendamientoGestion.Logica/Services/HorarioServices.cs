using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;

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
        var usuario = await _usuarioRepository.GetByIdAsync(horarioDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        var horario = new Horario
        {
            titulo = TituloParaCrear(horarioDto),
            fechaInicio = horarioDto.FechaInicio,
            fechaFin = horarioDto.FechaFin,
            horaInicio = horarioDto.HoraInicio,
            horaFin = horarioDto.HoraFin,
            cupos = Cupos.ValidarOpcionalHorario(horarioDto.Cupos),
            espacio = Aula.Normalizar(horarioDto.Espacio),
            estado = horarioDto.Estado,
            Usuario_idUsuario = usuario.idUsuario
        };

        var horarioCreado = await _horarioRepository.CreateAsync(horario);
        return Map(horarioCreado, usuario);
    }

    public async Task<HorarioResponseDto> GetHorarioByIdAsync(int id)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        // El repositorio ya incluye Usuario: se evita una consulta extra por horario (N+1).
        return Map(horario, horario.Usuario);
    }

    public async Task<List<HorarioResponseDto>> GetAllHorariosAsync()
    {
        var horarios = await _horarioRepository.GetAllAsync();
        return horarios.Select(h => Map(h, h.Usuario)).ToList();
    }

    public async Task<HorarioResponseDto> UpdateHorarioAsync(int id, HorarioCreateDto horarioDto)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(horarioDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        horario.titulo = TituloParaActualizar(horario, horarioDto);
        horario.fechaInicio = horarioDto.FechaInicio;
        horario.fechaFin = horarioDto.FechaFin;
        horario.horaInicio = horarioDto.HoraInicio;
        horario.horaFin = horarioDto.HoraFin;
        horario.cupos = Cupos.ValidarOpcionalHorario(horarioDto.Cupos);
        horario.espacio = Aula.Normalizar(horarioDto.Espacio);
        horario.estado = horarioDto.Estado;
        horario.Usuario_idUsuario = usuario.idUsuario;
        horario.Usuario = usuario;

        await _horarioRepository.UpdateAsync(horario);
        return Map(horario, usuario);
    }

    public async Task<bool> DeleteHorarioAsync(int id)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario == null)
        {
            throw new NotFoundException("Horario no encontrado");
        }

        try
        {
            return await _horarioRepository.DeleteAsync(id);
        }
        catch (DbUpdateException ex)
        {
            // FK Tutoria.Horario_idHorario es NO ACTION.
            throw new ConflictException("No se puede eliminar el horario porque tiene tutorías asociadas", ex);
        }
    }

    public async Task<List<HorarioResponseDto>> GetHorariosByUsuarioAsync(int usuarioId)
    {
        var horarios = await _horarioRepository.GetByUsuarioAsync(usuarioId);
        return horarios.Select(h => Map(h, h.Usuario)).ToList();
    }

    public async Task<List<HorarioResponseDto>> GetHorariosDisponiblesAsync()
    {
        var horarios = await _horarioRepository.GetDisponiblesAsync();
        return horarios.Select(h => Map(h, h.Usuario)).ToList();
    }

    private static string? TituloEnviado(HorarioCreateDto dto) =>
        string.IsNullOrWhiteSpace(dto.Titulo) ? null : dto.Titulo.Trim();

    private static string Generar(HorarioCreateDto dto) =>
        TituloHorario.Generar(dto.Espacio, dto.FechaInicio, dto.FechaFin, dto.HoraInicio, dto.HoraFin);

    private static string TituloParaCrear(HorarioCreateDto dto) => TituloEnviado(dto) ?? Generar(dto);

    /// <summary>
    /// PUT sin título: si el guardado es el que generaría el servidor con los datos ANTERIORES, se regenera con
    /// los nuevos (así no queda desactualizado al cambiar espacio/fecha/hora); si es uno personalizado
    /// (escrito por un usuario o por MAUI), se conserva. Debe llamarse antes de sobrescribir los campos.
    /// </summary>
    private static string TituloParaActualizar(Horario actual, HorarioCreateDto dto)
    {
        var enviado = TituloEnviado(dto);
        if (enviado != null) return enviado;

        var generadoAnterior = TituloHorario.Generar(actual.espacio, actual.fechaInicio, actual.fechaFin, actual.horaInicio, actual.horaFin);
        var eraGenerado = string.IsNullOrWhiteSpace(actual.titulo) || string.Equals(actual.titulo, generadoAnterior, StringComparison.Ordinal);
        return eraGenerado ? Generar(dto) : actual.titulo;
    }

    private static HorarioResponseDto Map(Horario horario, Usuario? usuario) => new()
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
        UsuarioId = horario.Usuario_idUsuario,
        // Dueño eliminado (Usuario_idUsuario null): id y nombres null, la UI muestra "Usuario eliminado".
        UsuarioNombre = horario.Usuario_idUsuario == null ? null : usuario?.nombres ?? "N/A",
        UsuarioApellidos = horario.Usuario_idUsuario == null ? null : usuario?.apellidos ?? "N/A",
        UsuarioCorreo = horario.Usuario_idUsuario == null ? null : usuario?.correo ?? "N/A"
    };
}
