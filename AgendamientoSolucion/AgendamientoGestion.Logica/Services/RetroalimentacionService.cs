using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Exceptions;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Services;

public class RetroalimentacionService : IRetroalimentacionService
{
    private readonly IRetroalimentacionRepository _retroalimentacionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITutoriaRepository _tutoriaRepository;

    public RetroalimentacionService(IRetroalimentacionRepository retroalimentacionRepository, IUsuarioRepository usuarioRepository, ITutoriaRepository tutoriaRepository)
    {
        _retroalimentacionRepository = retroalimentacionRepository;
        _usuarioRepository = usuarioRepository;
        _tutoriaRepository = tutoriaRepository;
    }

    public async Task<RetroalimentacionResponseDto> CreateRetroalimentacionAsync(RetroalimentacionCreateDto retroalimentacionDto)
    {
        // Verificar que el usuario existe
        var usuario = await _usuarioRepository.GetByIdAsync(retroalimentacionDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Verificar que la tutoría existe
        var tutoria = await _tutoriaRepository.GetByIdAsync(retroalimentacionDto.TutoriaId);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        // Validar que la calificación esté en el rango correcto
        if (retroalimentacionDto.Calificacion < 1 || retroalimentacionDto.Calificacion > 5)
        {
            throw new ValidationException("La calificación debe estar entre 1 y 5");
        }

        // Crear retroalimentación
        var retroalimentacion = new Retroalimentacion
        {
            comentario = retroalimentacionDto.Comentario,
            calificacion = retroalimentacionDto.Calificacion,
            fecha = DateTime.Now,
            Tutoria_idTutoria = retroalimentacionDto.TutoriaId,
            Usuario_idUsuario = retroalimentacionDto.UsuarioId
        };

        var retroalimentacionCreada = await _retroalimentacionRepository.CreateAsync(retroalimentacion);

        return new RetroalimentacionResponseDto
        {
            IdRetroalimentacion = retroalimentacionCreada.idRetroalimentacion,
            Comentario = retroalimentacionCreada.comentario,
            Calificacion = retroalimentacionCreada.calificacion,
            Fecha = retroalimentacionCreada.fecha,
            UsuarioNombre = usuario.nombres,
            UsuarioApellidos = usuario.apellidos,
            TutoriaTema = tutoria.tema,
            TutoriaIdioma = tutoria.idioma,
            TutoriaNivel = tutoria.nivel
        };
    }

    public async Task<RetroalimentacionResponseDto> GetRetroalimentacionByIdAsync(int id)
    {
        var retroalimentacion = await _retroalimentacionRepository.GetByIdAsync(id);
        if (retroalimentacion == null)
        {
            throw new NotFoundException("Retroalimentación no encontrada");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(retroalimentacion.Usuario_idUsuario);
        var tutoria = await _tutoriaRepository.GetByIdAsync(retroalimentacion.Tutoria_idTutoria);

        return new RetroalimentacionResponseDto
        {
            IdRetroalimentacion = retroalimentacion.idRetroalimentacion,
            Comentario = retroalimentacion.comentario,
            Calificacion = retroalimentacion.calificacion,
            Fecha = retroalimentacion.fecha,
            UsuarioNombre = usuario?.nombres ?? "N/A",
            UsuarioApellidos = usuario?.apellidos ?? "N/A",
            TutoriaTema = tutoria?.tema ?? "N/A",
            TutoriaIdioma = tutoria?.idioma ?? "N/A",
            TutoriaNivel = tutoria?.nivel ?? "N/A"
        };
    }

    public async Task<List<RetroalimentacionResponseDto>> GetAllRetroalimentacionesAsync()
    {
        var retroalimentaciones = await _retroalimentacionRepository.GetAllAsync();
        var retroalimentacionesDto = new List<RetroalimentacionResponseDto>();

        foreach (var retroalimentacion in retroalimentaciones)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(retroalimentacion.Usuario_idUsuario);
            var tutoria = await _tutoriaRepository.GetByIdAsync(retroalimentacion.Tutoria_idTutoria);
            
            retroalimentacionesDto.Add(new RetroalimentacionResponseDto
            {
                IdRetroalimentacion = retroalimentacion.idRetroalimentacion,
                Comentario = retroalimentacion.comentario,
                Calificacion = retroalimentacion.calificacion,
                Fecha = retroalimentacion.fecha,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A",
                TutoriaTema = tutoria?.tema ?? "N/A",
                TutoriaIdioma = tutoria?.idioma ?? "N/A",
                TutoriaNivel = tutoria?.nivel ?? "N/A"
            });
        }

        return retroalimentacionesDto;
    }

    public async Task<RetroalimentacionResponseDto> UpdateRetroalimentacionAsync(int id, RetroalimentacionCreateDto retroalimentacionDto)
    {
        var retroalimentacion = await _retroalimentacionRepository.GetByIdAsync(id);
        if (retroalimentacion == null)
        {
            throw new NotFoundException("Retroalimentación no encontrada");
        }

        // Verificar que el usuario existe
        var usuario = await _usuarioRepository.GetByIdAsync(retroalimentacionDto.UsuarioId);
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado");
        }

        // Verificar que la tutoría existe
        var tutoria = await _tutoriaRepository.GetByIdAsync(retroalimentacionDto.TutoriaId);
        if (tutoria == null)
        {
            throw new NotFoundException("Tutoría no encontrada");
        }

        // Validar que la calificación esté en el rango correcto
        if (retroalimentacionDto.Calificacion < 1 || retroalimentacionDto.Calificacion > 5)
        {
            throw new ValidationException("La calificación debe estar entre 1 y 5");
        }

        // Actualizar datos
        retroalimentacion.comentario = retroalimentacionDto.Comentario;
        retroalimentacion.calificacion = retroalimentacionDto.Calificacion;
        retroalimentacion.Tutoria_idTutoria = retroalimentacionDto.TutoriaId;
        retroalimentacion.Usuario_idUsuario = retroalimentacionDto.UsuarioId;

        await _retroalimentacionRepository.UpdateAsync(retroalimentacion);

        return new RetroalimentacionResponseDto
        {
            IdRetroalimentacion = retroalimentacion.idRetroalimentacion,
            Comentario = retroalimentacion.comentario,
            Calificacion = retroalimentacion.calificacion,
            Fecha = retroalimentacion.fecha,
            UsuarioNombre = usuario.nombres,
            UsuarioApellidos = usuario.apellidos,
            TutoriaTema = tutoria.tema,
            TutoriaIdioma = tutoria.idioma,
            TutoriaNivel = tutoria.nivel
        };
    }

    public async Task<bool> DeleteRetroalimentacionAsync(int id)
    {
        var retroalimentacion = await _retroalimentacionRepository.GetByIdAsync(id);
        if (retroalimentacion == null)
        {
            throw new NotFoundException("Retroalimentación no encontrada");
        }

        return await _retroalimentacionRepository.DeleteAsync(id);
    }

    public async Task<List<RetroalimentacionResponseDto>> GetRetroalimentacionesByTutoriaAsync(int tutoriaId)
    {
        var retroalimentaciones = await _retroalimentacionRepository.GetByTutoriaAsync(tutoriaId);
        var retroalimentacionesDto = new List<RetroalimentacionResponseDto>();

        foreach (var retroalimentacion in retroalimentaciones)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(retroalimentacion.Usuario_idUsuario);
            var tutoria = await _tutoriaRepository.GetByIdAsync(retroalimentacion.Tutoria_idTutoria);
            
            retroalimentacionesDto.Add(new RetroalimentacionResponseDto
            {
                IdRetroalimentacion = retroalimentacion.idRetroalimentacion,
                Comentario = retroalimentacion.comentario,
                Calificacion = retroalimentacion.calificacion,
                Fecha = retroalimentacion.fecha,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A",
                TutoriaTema = tutoria?.tema ?? "N/A",
                TutoriaIdioma = tutoria?.idioma ?? "N/A",
                TutoriaNivel = tutoria?.nivel ?? "N/A"
            });
        }

        return retroalimentacionesDto;
    }

    public async Task<List<RetroalimentacionResponseDto>> GetRetroalimentacionesByUsuarioAsync(int usuarioId)
    {
        var retroalimentaciones = await _retroalimentacionRepository.GetByUsuarioAsync(usuarioId);
        var retroalimentacionesDto = new List<RetroalimentacionResponseDto>();

        foreach (var retroalimentacion in retroalimentaciones)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(retroalimentacion.Usuario_idUsuario);
            var tutoria = await _tutoriaRepository.GetByIdAsync(retroalimentacion.Tutoria_idTutoria);
            
            retroalimentacionesDto.Add(new RetroalimentacionResponseDto
            {
                IdRetroalimentacion = retroalimentacion.idRetroalimentacion,
                Comentario = retroalimentacion.comentario,
                Calificacion = retroalimentacion.calificacion,
                Fecha = retroalimentacion.fecha,
                UsuarioNombre = usuario?.nombres ?? "N/A",
                UsuarioApellidos = usuario?.apellidos ?? "N/A",
                TutoriaTema = tutoria?.tema ?? "N/A",
                TutoriaIdioma = tutoria?.idioma ?? "N/A",
                TutoriaNivel = tutoria?.nivel ?? "N/A"
            });
        }

        return retroalimentacionesDto;
    }

}
