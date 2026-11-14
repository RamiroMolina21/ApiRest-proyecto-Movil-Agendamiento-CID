using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces;

public interface IRetroalimentacionService {
    /// <summary>Docente/Tutor: usuarioId = el suyo (403) y deben participar en la tutoría (403). 409 si ya existe una del mismo usuario.</summary>
    Task<RetroalimentacionResponseDto> CreateRetroalimentacionAsync(RetroalimentacionCreateDto retroalimentacionDto, AgendamientoGestion.Logica.Seguridad.Solicitante solicitante);
    Task<RetroalimentacionResponseDto> GetRetroalimentacionByIdAsync(int id);
    Task<List<RetroalimentacionResponseDto>> GetAllRetroalimentacionesAsync();
    Task<RetroalimentacionResponseDto> UpdateRetroalimentacionAsync(int id, RetroalimentacionCreateDto retroalimentacionDto);
    Task<bool> DeleteRetroalimentacionAsync(int id);
    Task<List<RetroalimentacionResponseDto>> GetRetroalimentacionesByTutoriaAsync(int tutoriaId, AgendamientoGestion.Logica.Seguridad.Solicitante solicitante);
    Task<List<RetroalimentacionResponseDto>> GetRetroalimentacionesByUsuarioAsync(int usuarioId);
}
