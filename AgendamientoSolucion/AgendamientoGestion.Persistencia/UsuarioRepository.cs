using AgendamientoGestion.Entidades.Constants;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia;

public class UsuarioRepository : IUsuarioRepository
{
    /// <summary>
    /// Collation de búsqueda: insensible a mayúsculas y a tildes (y trata ñ como n), misma página de códigos (1252)
    /// que la collation de las columnas (Modern_Spanish_CI_AS), por lo que no hay pérdida de caracteres.
    /// Se aplica solo en la consulta (COLLATE en el WHERE): no cambia el esquema.
    /// </summary>
    public const string CollationBusqueda = "Latin1_General_100_CI_AI";

    /// <summary>Carácter de escape para LIKE: los comodines que escriba el usuario se buscan literalmente.</summary>
    public const string EscapeLike = "\\";

    private readonly AgendamientoDbContext _context;

    public UsuarioRepository(AgendamientoDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario> GetByIdAsync(int id)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.idUsuario == id);
    }

    public async Task<Usuario> GetByEmailAsync(string email)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.correo == email);
    }

    public async Task<Usuario> GetByNombreApellidosCorreoAsync(string nombres, string apellidos, string correo)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.nombres == nombres && u.apellidos == apellidos && u.correo == correo);
    }

    public async Task<List<Usuario>> GetAllAsync()
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .ToListAsync();
    }

    public async Task<Usuario> UpdateAsync(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return false;

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return true;
    }

    public const string NombreAnonimizado = "Usuario eliminado";
    public const string DominioAnonimizado = "usuario-eliminado.invalid";

    public async Task<ResultadoEliminacionUsuario?> EliminarConDependenciasAsync(int id)
    {
        // Debe ejecutarse dentro de una transacción abierta por el llamador (IUnidadDeTrabajo): todo o nada.
        var usuario = await _context.Usuarios.AsNoTracking()
            .Where(u => u.idUsuario == id)
            .Select(u => new { u.idUsuario, u.correo })
            .FirstOrDefaultAsync();
        if (usuario == null)
            return null;

        var r = new ResultadoEliminacionUsuario();

        // 1) Encuestas dirigidas a este usuario (EncuestaSatisfaccion no tiene FK a Usuario: se enlaza por correo).
        //    Sin responder → se borran (invitación a una persona que ya no existe). Respondidas → se conservan
        //    (calificación de la tutoría) pero se sustituyen nombre y correo (dato personal del eliminado). En la BD
        //    real ambas columnas son NOT NULL: se usan marcadores, el correo único por fila y en el dominio reservado
        //    .invalid (RFC 2606), para no chocar con el índice único (tutoría, correo) ni parecer una dirección real.
        r.EncuestasEliminadas = await _context.EncuestasSatisfaccion
            .Where(e => e.estudianteCorreo == usuario.correo && !e.tokenUsado)
            .ExecuteDeleteAsync();
        r.EncuestasAnonimizadas = await _context.EncuestasSatisfaccion
            .Where(e => e.estudianteCorreo == usuario.correo && e.tokenUsado)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.estudianteCorreo, e => "eliminado-" + e.idEncuesta.ToString() + "@" + DominioAnonimizado)
                .SetProperty(e => e.estudianteNombre, NombreAnonimizado));

        // 2) Datos propios: sus inscripciones como estudiante y sus notificaciones.
        r.InscripcionesEliminadas = await _context.TutoriaEstudiantes
            .Where(te => te.Usuario_idUsuario == id).ExecuteDeleteAsync();
        r.NotificacionesEliminadas = await _context.Notificaciones
            .Where(n => n.Usuario_idUsuario == id).ExecuteDeleteAsync();

        // 3) Horarios propios: sin tutorías → se borran; con tutorías (historial o pendientes) → se conservan sin dueño.
        r.HorariosEliminados = await _context.Horarios
            .Where(h => h.Usuario_idUsuario == id && !_context.Tutorias.Any(t => t.Horario_idHorario == h.idHorario))
            .ExecuteDeleteAsync();
        r.HorariosConservados = await _context.Horarios
            .Where(h => h.Usuario_idUsuario == id)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Usuario_idUsuario, (int?)null));

        // 4) Datos compartidos: se conservan desvinculados (FK a null).
        r.TutoriasDesasignadas = await _context.Tutorias
            .Where(t => t.Usuario_idUsuario == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Usuario_idUsuario, (int?)null));
        r.RetroalimentacionesConservadas = await _context.Retroalimentaciones
            .Where(x => x.Usuario_idUsuario == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Usuario_idUsuario, (int?)null));
        r.InformesConservados = await _context.Informes
            .Where(x => x.Usuario_idUsuario == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Usuario_idUsuario, (int?)null));
        r.AtendidosConservados = await _context.AsuntosAtendidos
            .Where(x => x.Usuario_idUsuario == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Usuario_idUsuario, (int?)null));

        // Solicitudes de restablecimiento de contraseña (dato propio; la FK también es CASCADE).
        await _context.RestablecimientosContrasena.Where(x => x.Usuario_idUsuario == id).ExecuteDeleteAsync();

        // 5) El usuario. Si quedara alguna referencia no contemplada, la FK hace fallar esto y la transacción se deshace.
        await _context.Usuarios.Where(u => u.idUsuario == id).ExecuteDeleteAsync();
        return r;
    }

    public async Task<(List<Usuario> Usuarios, int Total)> BuscarPaginadoAsync(
        IReadOnlyList<string> terminos, string? tipoRol, bool? activo, int skip, int take)
    {
        IQueryable<Usuario> query = _context.Usuarios;

        if (!string.IsNullOrEmpty(tipoRol))
        {
            query = query.Where(u => u.Rol.tipoRol == tipoRol);
        }

        if (activo.HasValue)
        {
            query = query.Where(u => u.activo == activo.Value);
        }

        foreach (var termino in terminos)
        {
            var patron = "%" + EscaparLike(termino) + "%";
            query = query.Where(u =>
                EF.Functions.Like(EF.Functions.Collate(u.nombres, CollationBusqueda), patron, EscapeLike) ||
                EF.Functions.Like(EF.Functions.Collate(u.apellidos, CollationBusqueda), patron, EscapeLike) ||
                EF.Functions.Like(EF.Functions.Collate(u.correo, CollationBusqueda), patron, EscapeLike));
        }

        var total = await query.CountAsync();
        var usuarios = total == 0 || skip >= total
            ? new List<Usuario>()
            : await query
                .Include(u => u.Rol)
                .OrderBy(u => u.apellidos).ThenBy(u => u.nombres).ThenBy(u => u.idUsuario)
                .Skip(skip).Take(take)
                .ToListAsync();

        return (usuarios, total);
    }

    /// <summary>Escapa \, %, _ y [ (este último es comodín en SQL Server) con el carácter <see cref="EscapeLike"/>.</summary>
    private static string EscaparLike(string texto) => texto
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_")
        .Replace("[", "\\[");

    public async Task<bool?> GetActivoAsync(int id) =>
        await _context.Usuarios.AsNoTracking()
            .Where(u => u.idUsuario == id)
            .Select(u => (bool?)u.activo)
            .FirstOrDefaultAsync();

    public async Task<(bool Activo, DateTime? CredencialesCambiadasEn)?> GetEstadoCredencialesAsync(int id)
    {
        var r = await _context.Usuarios.AsNoTracking()
            .Where(u => u.idUsuario == id)
            .Select(u => new { u.activo, u.credencialesCambiadasEn })
            .FirstOrDefaultAsync();
        return r == null ? null : (r.activo, r.credencialesCambiadasEn);
    }

    public Task<int> ContarAdministradoresActivosAsync() =>
        _context.Usuarios.CountAsync(u => u.activo && u.Rol.tipoRol == Roles.Administrador);

    public async Task<List<Usuario>> GetByRolAsync(string tipoRol)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Rol.tipoRol == tipoRol)
            .ToListAsync();
    }
}
