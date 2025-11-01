using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces;

public interface IRetroalimentacionService {
    Task<RetroalimentacionResponseDto> CreateRetroalimentacionAsync(RetroalimentacionCreateDto retroalimentacionDto);
    Task<RetroalimentacionResponseDto> GetRetroalimentacionByIdAsync(int id);
    Task<List<RetroalimentacionResponseDto>> GetAllRetroalimentacionesAsync();
    Task<RetroalimentacionResponseDto> UpdateRetroalimentacionAsync(int id, RetroalimentacionCreateDto retroalimentacionDto);
    Task<bool> DeleteRetroalimentacionAsync(int id);
    Task<List<RetroalimentacionResponseDto>> GetRetroalimentacionesByTutoriaAsync(int tutoriaId);
    Task<List<RetroalimentacionResponseDto>> GetRetroalimentacionesByUsuarioAsync(int usuarioId);
}
