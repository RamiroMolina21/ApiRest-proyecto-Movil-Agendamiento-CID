namespace AgendamientoGestion.Logica.Models
{
    public class SmtpSettings
    {
        public string Remitente { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
    }
}
