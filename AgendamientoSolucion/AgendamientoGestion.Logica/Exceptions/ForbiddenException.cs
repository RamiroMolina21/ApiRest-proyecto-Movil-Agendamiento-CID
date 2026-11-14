namespace AgendamientoGestion.Logica.Exceptions
{
    /// <summary>El usuario autenticado no tiene permiso para realizar esta operación (HTTP 403).</summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }

        public ForbiddenException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
