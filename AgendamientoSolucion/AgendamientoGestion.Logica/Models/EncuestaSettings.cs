namespace AgendamientoGestion.Logica.Models
{
    /// <summary>Sección "Encuesta" de configuración.</summary>
    public class EncuestaSettings
    {
        public const string SectionName = "Encuesta";

        /// <summary>
        /// URL pública de la página de encuesta de la web React. Se le agrega ?token=... al construir el enlace.
        /// Ej.: http://localhost:5173/encuesta (desarrollo) o https://midominio/encuesta (producción).
        /// </summary>
        public string UrlBase { get; set; } = "http://localhost:5173/encuesta";

        public string ConstruirEnlace(string token)
        {
            var separador = UrlBase.Contains('?') ? "&" : "?";
            return $"{UrlBase}{separador}token={Uri.EscapeDataString(token)}";
        }
    }
}
