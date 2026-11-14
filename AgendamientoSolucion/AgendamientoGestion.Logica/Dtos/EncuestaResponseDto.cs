using System;

namespace AgendamientoGestion.Logica.Dtos
{
    public class EncuestaResponseDto
    {
        public int IdEncuesta { get; set; }
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string EstudianteNombre { get; set; }
        public string EstudianteCorreo { get; set; }
        public bool TokenUsado { get; set; }
        public int TutoriaId { get; set; }
    }
}
