using AgendamientoGestion.Entidades.Constants;

namespace AgendamientoGestion.Logica.Seguridad;

/// <summary>
/// Usuario autenticado que hace la petición (sale de los claims "sub" y "rol" del JWT).
/// La capa web lo construye; la lógica de negocio lo usa para aplicar reglas de propiedad.
/// </summary>
public sealed record Solicitante(int UsuarioId, string Rol)
{
    /// <summary>Administrador o Auxiliar: sin restricción de propiedad.</summary>
    public bool EsGestion => Roles.Es(Rol, Roles.Administrador) || Roles.Es(Rol, Roles.Auxiliar);

    public bool EsDocente => Roles.Es(Rol, Roles.Docente);

    public bool EsTutor => Roles.Es(Rol, Roles.Tutor);
}
