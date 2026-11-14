using System.ComponentModel.DataAnnotations;

namespace AgendamientoGestion.Logica.Dtos;

/// <summary>Cuerpo de PATCH /api/Usuario/{id}/estado.</summary>
public class UsuarioEstadoDto
{
    [Required(ErrorMessage = "El campo activo es obligatorio (true o false)")]
    public bool? Activo { get; set; }
}
