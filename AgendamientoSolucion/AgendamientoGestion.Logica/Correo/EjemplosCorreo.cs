namespace AgendamientoGestion.Logica.Correo;

/// <summary>
/// Datos de ejemplo para PREVISUALIZAR cada plantilla sin enviar correos (endpoint /dev/correos en Development y
/// test PlantillasCorreoTests, que además escribe los .html/.txt en una carpeta). Ficticios; nombres con tildes y
/// textos largos a propósito para probar el diseño.
/// </summary>
public static class EjemplosCorreo
{
    private static readonly DateTime Fecha = new(2026, 11, 20, 14, 30, 0);

    public static IReadOnlyList<IModeloCorreo> Todos() => new IModeloCorreo[]
    {
        new NuevaTutoriaCorreo("María José Núñez Peña", 123, "Inglés", "B1", "Conversación: viajes y transporte",
            "Presencial", Fecha, "Bloque B · Aula 204"),
        new RecordatorioTutoriaCorreo("María José Núñez Peña", Fecha, "Inglés", "B1", "Conversación: viajes y transporte",
            "Presencial", "Bloque B · Aula 204"),
        new CambioTutoriaCorreo("María José Núñez Peña", 123, Fecha.AddDays(1), "Inglés", "B1",
            "Conversación: viajes y transporte", "Virtual", "Sala virtual Meet", "Laura Valentina Herrera Díaz",
            "Reprogramación por actividad institucional",
            new[] { "Fecha cambió de 20/11/2026 a 21/11/2026", "Modalidad cambió de Presencial a Virtual" }),
        new CancelacionTutoriaCorreo("María José Núñez Peña", 123, Fecha, "Francés", "A1", "Pronunciación básica",
            "El docente no estará disponible"),
        new EncuestaSatisfaccionCorreo("María José", "Inglés", "B1", "Conversación: viajes y transporte",
            "https://tutorias.ejemplo.edu.co/encuesta?token=0123456789abcdef0123456789abcdef")
    };

    public static IModeloCorreo? Buscar(string plantilla) =>
        Todos().FirstOrDefault(m => string.Equals(m.Plantilla, plantilla, StringComparison.OrdinalIgnoreCase));
}
