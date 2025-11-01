using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces;

public interface IHorarioServices
{
    Task<HorarioResponseDto> CreateHorarioAsync(HorarioCreateDto horarioDto);
    Task<HorarioResponseDto> GetHorarioByIdAsync(int id);
    Task<List<HorarioResponseDto>> GetAllHorariosAsync();
    Task<HorarioResponseDto> UpdateHorarioAsync(int id, HorarioCreateDto horarioDto);
    Task<bool> DeleteHorarioAsync(int id);
    Task<List<HorarioResponseDto>> GetHorariosByUsuarioAsync(int usuarioId);
    Task<List<HorarioResponseDto>> GetHorariosDisponiblesAsync();
}
