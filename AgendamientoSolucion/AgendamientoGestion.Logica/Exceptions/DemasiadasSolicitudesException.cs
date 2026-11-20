namespace AgendamientoGestion.Logica.Exceptions;

/// <summary>429 Too Many Requests con cabecera Retry-After (segundos).</summary>
public class DemasiadasSolicitudesException : Exception
{
    public int ReintentarEnSegundos { get; }

    public DemasiadasSolicitudesException(string message, int reintentarEnSegundos) : base(message)
    {
        ReintentarEnSegundos = Math.Max(1, reintentarEnSegundos);
    }
}