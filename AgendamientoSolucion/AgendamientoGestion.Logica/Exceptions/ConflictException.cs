namespace AgendamientoGestion.Logica.Exceptions
{
    /// <summary>La operación entra en conflicto con el estado actual de los datos, p. ej. un correo duplicado (HTTP 409).</summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
