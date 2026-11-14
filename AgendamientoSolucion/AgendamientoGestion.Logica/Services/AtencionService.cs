using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.Interfaces;

namespace AgendamientoGestion.Logica.Services;

public class AtencionService : IAtencionService
{
    private readonly IAsuntoAtendidoRepository _repositorio;
    private readonly TimeProvider _reloj;

    public AtencionService(IAsuntoAtendidoRepository repositorio, TimeProvider reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    public async Task<List<AsuntoAtendidoDto>> GetAtendidosAsync(string? tipo)
    {
        var canonico = string.IsNullOrWhiteSpace(tipo) ? null : ValidarTipo(tipo);
        var atendidos = await _repositorio.GetAsync(canonico);
        return atendidos.Select(Map).ToList();
    }

    public async Task<List<AsuntoAtendidoDto>> MarcarAtendidosAsync(MarcarAtendidosDto dto, int usuarioId)
    {
        var tipo = ValidarTipo(dto.Tipo);
        var claves = (dto.Claves ?? new List<int>()).Distinct().ToList();

        if (claves.Count == 0)
            throw new ValidationException("Debe indicar al menos una clave");
        if (claves.Count > MarcarAtendidosDto.MaximoClaves)
            throw new ValidationException($"No se pueden marcar más de {MarcarAtendidosDto.MaximoClaves} claves por petición");
        if (claves.Any(c => c <= 0))
            throw new ValidationException("Las claves deben ser ids positivos");

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var nuevos = claves
            .Select(c => new AsuntoAtendido { tipo = tipo, clave = c, Usuario_idUsuario = usuarioId, fecha = ahora })
            .ToList();
        await _repositorio.AgregarFaltantesAsync(nuevos);

        var resultado = await _repositorio.GetAsync(tipo, claves);
        return resultado.Select(Map).ToList();
    }

    public async Task DesmarcarAsync(string tipo, int clave)
    {
        await _repositorio.DeleteAsync(ValidarTipo(tipo), clave);
    }

    private static string ValidarTipo(string? tipo) =>
        TiposAsunto.Normalizar(tipo)
        ?? throw new ValidationException($"tipo no válido. Valores admitidos: {string.Join(", ", TiposAsunto.Todos)}");

    private static AsuntoAtendidoDto Map(AsuntoAtendido a) => new()
    {
        Tipo = a.tipo,
        Clave = a.clave,
        UsuarioId = a.Usuario_idUsuario,
        UsuarioNombre = a.Usuario == null ? null : $"{a.Usuario.nombres} {a.Usuario.apellidos}".Trim(),
        // SQL Server devuelve Kind=Unspecified: se marca UTC para que el JSON lleve "Z".
        Fecha = DateTime.SpecifyKind(a.fecha, DateTimeKind.Utc)
    };
}
