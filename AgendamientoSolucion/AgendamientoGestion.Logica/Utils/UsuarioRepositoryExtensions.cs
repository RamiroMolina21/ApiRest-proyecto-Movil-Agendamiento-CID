using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Persistencia.Interfaces;

namespace AgendamientoGestion.Logica.Utils;

public static class UsuarioRepositoryExtensions
{
    /// <summary>
    /// Usuario por id para FKs que pueden ser null (Tutoria/Horario/Retroalimentacion/Informe/AsuntoAtendido.Usuario_idUsuario
    /// quedan en null cuando el usuario se elimina). null si el id es null o el usuario no existe.
    /// </summary>
    public static async Task<Usuario?> BuscarAsync(this IUsuarioRepository repositorio, int? id) =>
        id is int valor ? await repositorio.GetByIdAsync(valor) : null;
}