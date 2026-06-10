using System;

namespace AgendamientoGestion.Entidades.Entities
{
    public class EncuestaSatisfaccion
    {
        public int idEncuesta { get; set; }
        public int calificacion { get; set; }
        public string? comentario { get; set; }
        public DateTime? fechaRespuesta { get; set; }
        public string tokenRespuesta { get; set; }
        public bool tokenUsado { get; set; } = false;
        public string? estudianteNombre { get; set; }
        public string? estudianteCorreo { get; set; }
        public int Tutoria_idTutoria { get; set; }

        // Propiedad de navegación
        public Tutoria Tutoria { get; set; }
    }
}
