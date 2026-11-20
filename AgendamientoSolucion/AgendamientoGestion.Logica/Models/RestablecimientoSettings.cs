namespace AgendamientoGestion.Logica.Models;

/// <summary>Sección "Web": URL pública de la web React (enlaces de los correos). En Render: Web__UrlBase.</summary>
public class WebSettings
{
    public const string SectionName = "Web";
    public string UrlBase { get; set; } = "http://localhost:5173";

    public string Enlace(string ruta, string parametro, string valor) =>
        $"{UrlBase.TrimEnd('/')}/{ruta.TrimStart('/')}?{parametro}={Uri.EscapeDataString(valor)}";
}

/// <summary>Sección "RestablecimientoContrasena".</summary>
public class RestablecimientoSettings
{
    public const string SectionName = "RestablecimientoContrasena";

    /// <summary>Vida del enlace en minutos.</summary>
    public int MinutosValidez { get; set; } = 60;

    /// <summary>Solicitudes por correo (exista o no) en la ventana; al superarlo, 429.</summary>
    public int SolicitudesPorCorreo { get; set; } = 5;
    public int VentanaSegundos { get; set; } = 900;

    /// <summary>POST olvide-contrasena por IP en la ventana (rate limiter "olvide-contrasena").</summary>
    public int SolicitudesPorIp { get; set; } = 5;

    /// <summary>GET validar + POST restablecer-contrasena por IP en la ventana (rate limiter "restablecer-contrasena").</summary>
    public int IntentosRestablecerPorIp { get; set; } = 20;

    /// <summary>
    /// SOLO DESARROLLO: escribe el enlace en el log para probar sin correo. Se ignora fuera de Development
    /// (lo comprueba Program.cs). Por defecto false.
    /// </summary>
    public bool RegistrarEnlaceEnLogDesarrollo { get; set; }
}