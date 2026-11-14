using AgendamientoGestion.Entidades.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Persistencia.Interfaces;

public interface ITutoriaRepository
{
    Task<Tutoria> CreateAsync(Tutoria tutoria);
    Task<Tutoria> GetByIdAsync(int id);
    Task<List<Tutoria>> GetAllAsync();
    Task<Tutoria> UpdateAsync(Tutoria tutoria);
    Task<bool> DeleteAsync(int id);
    /// <summary>
    /// Elimina la tutoría y todos sus dependientes (Informe, Retroalimentacion, TutoriaEstudiante,
    /// EncuestaSatisfaccion) en una sola transacción. Devuelve false si no existe.
    /// </summary>
    Task<bool> DeleteWithDependenciesAsync(int id);
    Task<List<Tutoria>> GetByUsuarioAsync(int usuarioId);
    Task<List<Tutoria>> GetByHorarioAsync(int horarioId);
    Task<List<Tutoria>> GetByEstadoAsync(string estado);
    Task<List<Tutoria>> GetByEstadoAndUsuarioAsync(string estado, int usuarioId);
    /// <summary>Tutorías asignadas al usuario cuyo estado (en minúsculas) está en <paramref name="estadosLower"/>.</summary>
    Task<List<Tutoria>> GetByEstadosAndUsuarioAsync(IReadOnlyCollection<string> estadosLower, int usuarioId);
    /// <summary>Tutorías agendadas en horarios del usuario (Horario.Usuario_idUsuario), opcionalmente filtradas por estado.</summary>
    Task<List<Tutoria>> GetByDuenoHorarioAsync(int usuarioId, IReadOnlyCollection<string>? estadosLower = null);
    /// <summary>Bloqueo exclusivo de la fila de la tutoría hasta el fin de la transacción en curso.</summary>
    Task BloquearAsync(int tutoriaId);
    /// <summary>UPDATE condicional a "Finalizada" si no estaba finalizada/cancelada. false si no cambió.</summary>
    Task<bool> MarcarFinalizadaAsync(int tutoriaId);
    Task<List<Tutoria>> GetByFechaAsync(DateTime fecha);
    Task<List<Tutoria>> GetByIdiomaNivelAsync(string idioma, string nivel);
    Task<List<Tutoria>> GetProximasParaRecordatorioAsync(TimeSpan tiempoAntes);
}
