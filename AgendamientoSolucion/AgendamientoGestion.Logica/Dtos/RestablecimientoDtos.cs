namespace AgendamientoGestion.Logica.Dtos;

/// <summary>Cuerpo de POST /api/Auth/olvide-contrasena.</summary>
public class OlvideContrasenaDto
{
    public string? Correo { get; set; }
}

/// <summary>Cuerpo de POST /api/Auth/restablecer-contrasena.</summary>
public class RestablecerContrasenaDto
{
    public string? Token { get; set; }
    public string? ContrasenaNueva { get; set; }
}