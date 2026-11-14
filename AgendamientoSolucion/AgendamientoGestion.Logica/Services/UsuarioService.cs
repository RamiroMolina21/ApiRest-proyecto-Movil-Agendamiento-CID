using AgendamientoGestion.Entidades.Constants;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Utils;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendamientoGestion.Logica.Services;

public class UsuarioService : IUsuarioService
{
    private const string MensajeSoloEstudiantes =
        "Solo un Administrador puede crear, modificar o eliminar usuarios que no sean Estudiantes";

    public const string MensajeNoDesactivarseASiMismo = "No puede desactivar su propia cuenta";
    public const string MensajeUltimoAdministrador = "No se puede desactivar al último Administrador activo";

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IEstadoCuentaCache _estadoCuenta;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IRolRepository rolRepository,
        IUnidadDeTrabajo unidadDeTrabajo,
        IEstadoCuentaCache estadoCuenta)
    {
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
        _unidadDeTrabajo = unidadDeTrabajo;
        _estadoCuenta = estadoCuenta;
    }

    public async Task<UsuarioResponseDto> CreateUsuarioAsync(UsuarioCreateDto usuarioDto, string rolSolicitante)
    {
        EmailValidator.ValidateEmail(usuarioDto.Correo, "Correo");
        var correo = usuarioDto.Correo.Trim();

        var rol = await _rolRepository.GetByIdAsync(usuarioDto.RolId);
        if (rol == null)
        {
            throw new NotFoundException("El rol especificado no existe");
        }

        AsegurarPuedeGestionar(rolSolicitante, rol.tipoRol);

        if (await _usuarioRepository.GetByEmailAsync(correo) != null)
        {
            throw new ConflictException("Ya existe un usuario con este correo");
        }

        var usuario = new Usuario
        {
            nombres = usuarioDto.Nombres,
            apellidos = usuarioDto.Apellidos,
            correo = correo,
            contrasenaHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Contrasena),
            fechaRegistro = DateTime.Now,
            Rol_idRol = usuarioDto.RolId,
            activo = true
        };

        var usuarioCreado = await _usuarioRepository.CreateAsync(usuario);
        return Map(usuarioCreado, rol.tipoRol);
    }

    public async Task<UsuarioResponseDto> GetUsuarioByIdAsync(int id)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // El repositorio ya incluye Rol (antes se hacía una consulta extra por usuario).
        return Map(usuario, usuario.Rol?.tipoRol);
    }

    public async Task<List<UsuarioResponseDto>> GetAllUsuariosAsync()
    {
        var usuarios = await _usuarioRepository.GetAllAsync();
        return usuarios.Select(u => Map(u, u.Rol?.tipoRol)).ToList();
    }

    public async Task<PaginaDto<UsuarioResponseDto>> BuscarUsuariosAsync(UsuarioBusquedaDto busqueda)
    {
        var page = busqueda.Page ?? 1;
        var pageSize = busqueda.PageSize ?? UsuarioBusquedaDto.PageSizePorDefecto;
        if (page < 1)
            throw new ValidationException("page debe ser un entero mayor o igual a 1");
        if (pageSize < 1 || pageSize > UsuarioBusquedaDto.PageSizeMaximo)
            throw new ValidationException($"pageSize debe ser un entero entre 1 y {UsuarioBusquedaDto.PageSizeMaximo}");

        string? rol = null;
        if (!string.IsNullOrWhiteSpace(busqueda.Rol))
        {
            rol = Roles.Normalizar(busqueda.Rol)
                ?? throw new ValidationException($"rol no válido. Valores admitidos: {string.Join(", ", Roles.Todos)}");
        }

        var q = busqueda.Q?.Trim() ?? string.Empty;
        if (q.Length > UsuarioBusquedaDto.LongitudMaximaQ)
            throw new ValidationException($"q no puede exceder {UsuarioBusquedaDto.LongitudMaximaQ} caracteres");
        // Cada palabra debe aparecer en nombres, apellidos o correo: "ana pérez" encuentra a Ana (nombres) Pérez (apellidos).
        var terminos = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Evita desbordar int en Skip con page enormes: si se pasa del total, el repositorio devuelve vacío.
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var (usuarios, total) = await _usuarioRepository.BuscarPaginadoAsync(terminos, rol, busqueda.Activo, skip, pageSize);
        return new PaginaDto<UsuarioResponseDto>(usuarios.Select(u => Map(u, u.Rol?.tipoRol)).ToList(), total);
    }

    public async Task<UsuarioResponseDto> UpdateUsuarioAsync(int id, UsuarioUpdateDto usuarioDto, string rolSolicitante)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        EmailValidator.ValidateEmail(usuarioDto.Correo, "Correo");
        var correo = usuarioDto.Correo.Trim();

        var rol = await _rolRepository.GetByIdAsync(usuarioDto.RolId);
        if (rol == null)
        {
            throw new NotFoundException("El rol especificado no existe");
        }

        // Un no-Administrador solo puede editar Estudiantes y no puede cambiarles el rol a otro distinto.
        AsegurarPuedeGestionar(rolSolicitante, usuario.Rol?.tipoRol);
        AsegurarPuedeGestionar(rolSolicitante, rol.tipoRol);

        var otroConCorreo = await _usuarioRepository.GetByEmailAsync(correo);
        if (otroConCorreo != null && otroConCorreo.idUsuario != usuario.idUsuario)
        {
            throw new ConflictException("Ya existe un usuario con este correo");
        }

        usuario.nombres = usuarioDto.Nombres;
        usuario.apellidos = usuarioDto.Apellidos;
        usuario.correo = correo;
        usuario.Rol_idRol = usuarioDto.RolId;
        usuario.Rol = rol;

        // Solo se re-hashea si llega una contraseña nueva.
        if (!string.IsNullOrEmpty(usuarioDto.Contrasena))
        {
            usuario.contrasenaHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Contrasena);
        }

        await _usuarioRepository.UpdateAsync(usuario);
        return Map(usuario, rol.tipoRol);
    }

    public const string MensajeNoEliminarseASiMismo = "No puede eliminar su propio usuario";
    public const string MensajeUltimoAdministradorEliminar = "No se puede eliminar al último Administrador activo";

    /// <summary>
    /// Eliminación definitiva con todas sus dependencias en UNA transacción (ver UsuarioRepository.EliminarConDependenciasAsync):
    /// datos propios (inscripciones, notificaciones, horarios sin tutorías, encuestas sin responder) se borran;
    /// datos compartidos (tutorías, horarios con tutorías, retroalimentaciones, informes, atendidos) se conservan
    /// con el usuario a null. Nunca borra tutorías ni datos de otros usuarios.
    /// </summary>
    public async Task<ResultadoEliminacionUsuario> DeleteUsuarioAsync(int id, int solicitanteId, string rolSolicitante)
    {
        if (id == solicitanteId)
            throw new ValidationException(MensajeNoEliminarseASiMismo);

        var usuario = await _usuarioRepository.GetByIdAsync(id)
                      ?? throw new NotFoundException("Usuario no encontrado");

        AsegurarPuedeGestionar(rolSolicitante, usuario.Rol?.tipoRol);

        var esAdmin = Roles.Es(usuario.Rol?.tipoRol, Roles.Administrador);
        // Serializable solo si se borra un Administrador: el conteo y el borrado deben ser atómicos (igual que al desactivar).
        await using var tx = await _unidadDeTrabajo.IniciarTransaccionAsync(
            esAdmin ? System.Data.IsolationLevel.Serializable : System.Data.IsolationLevel.ReadCommitted);

        if (esAdmin && usuario.activo && await _usuarioRepository.ContarAdministradoresActivosAsync() <= 1)
            throw new ConflictException(MensajeUltimoAdministradorEliminar);

        ResultadoEliminacionUsuario resultado;
        try
        {
            resultado = await _usuarioRepository.EliminarConDependenciasAsync(id)
                        ?? throw new NotFoundException("Usuario no encontrado");
        }
        catch (DbUpdateException ex)
        {
            // No debería ocurrir: todas las FKs a Usuario están resueltas. Si aparece una tabla nueva sin contemplar,
            // la transacción se deshace entera (nada queda a medias) y se informa en vez de devolver 500.
            throw new ConflictException("No se pudo eliminar el usuario por una referencia no contemplada; no se modificó nada", ex);
        }

        await tx.CommitAsync();
        _estadoCuenta.Invalidar(id);
        return resultado;
    }

    public async Task<List<UsuarioResponseDto>> GetUsuariosByRolAsync(string tipoRol, bool incluirInactivos = false)
    {
        var usuarios = await _usuarioRepository.GetByRolAsync(tipoRol);
        return usuarios
            .Where(u => incluirInactivos || u.activo)
            .Select(u => Map(u, u.Rol?.tipoRol))
            .ToList();
    }

    public async Task<UsuarioResponseDto> CambiarEstadoAsync(int id, bool activo, int solicitanteId, string rolSolicitante)
    {
        // Serializable: el conteo de administradores activos y la actualización son atómicos frente a dos
        // administradores que se desactivan mutuamente a la vez (si no, podría quedar ninguno activo).
        await using var tx = await _unidadDeTrabajo.IniciarTransaccionAsync(System.Data.IsolationLevel.Serializable);

        var usuario = await _usuarioRepository.GetByIdAsync(id)
                      ?? throw new NotFoundException("Usuario no encontrado");

        AsegurarPuedeGestionar(rolSolicitante, usuario.Rol?.tipoRol);

        if (!activo)
        {
            // Primero la regla del último Administrador: es el motivo más grave (dejaría el sistema sin
            // administración) y es la que aplica cuando el único Administrador intenta desactivarse.
            if (usuario.activo && Roles.Es(usuario.Rol?.tipoRol, Roles.Administrador)
                && await _usuarioRepository.ContarAdministradoresActivosAsync() <= 1)
                throw new ConflictException(MensajeUltimoAdministrador);

            if (id == solicitanteId)
                throw new ConflictException(MensajeNoDesactivarseASiMismo);
        }

        if (usuario.activo != activo)
        {
            usuario.activo = activo;
            await _usuarioRepository.UpdateAsync(usuario);
        }

        await tx.CommitAsync();
        _estadoCuenta.Invalidar(id);
        return Map(usuario, usuario.Rol?.tipoRol);
    }

    /// <summary>
    /// Regla de negocio: el Administrador gestiona cualquier usuario; cualquier otro rol con acceso a la
    /// gestión (Auxiliar) solo puede gestionar Estudiantes. Evita que un Auxiliar se cree o promueva a
    /// Administrador (escalada de privilegios) sin romper el flujo de la web, donde el Auxiliar registra estudiantes.
    /// </summary>
    private static void AsegurarPuedeGestionar(string rolSolicitante, string? rolObjetivo)
    {
        if (Roles.Es(rolSolicitante, Roles.Administrador))
            return;

        if (!Roles.Es(rolObjetivo, Roles.Estudiante))
            throw new ForbiddenException(MensajeSoloEstudiantes);
    }

    internal static UsuarioResponseDto Map(Usuario usuario, string? tipoRol) => new()
    {
        IdUsuario = usuario.idUsuario,
        Nombres = usuario.nombres,
        Apellidos = usuario.apellidos,
        Correo = usuario.correo,
        FechaRegistro = usuario.fechaRegistro,
        TipoRol = tipoRol ?? "Sin rol",
        RolId = usuario.Rol_idRol,
        Activo = usuario.activo
    };
}
