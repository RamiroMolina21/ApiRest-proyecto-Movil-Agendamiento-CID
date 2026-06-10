namespace AgendamientoGestion.Logica.Dtos
{
    public class EncuestaEstadisticasDto
    {
        public int TotalEnviadas { get; set; }
        public int TotalRespondidas { get; set; }
        public double PromedioCalificacion { get; set; }
        public double TasaRespuesta { get; set; }
    }
}
