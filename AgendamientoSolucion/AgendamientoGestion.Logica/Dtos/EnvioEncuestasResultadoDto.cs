namespace AgendamientoGestion.Logica.Dtos
{
    public class EnvioEncuestasResultadoDto
    {
        /// <summary>Encuestas nuevas creadas en esta llamada.</summary>
        public int Enviadas { get; set; }

        /// <summary>Estudiantes omitidos porque ya tenían encuesta para esta tutoría.</summary>
        public int YaExistentes { get; set; }
    }
}
