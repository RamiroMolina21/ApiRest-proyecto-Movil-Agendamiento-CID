using AgendamientoGestion.Entidades.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agendamiento.Tests.Infrastructure;

/// <summary>Helpers para crear datos directamente en la BD de prueba y leer JSON.</summary>
public static class Datos
{
    /// <summary>Horario de HOY de 08:00 a 09:00 (las tutorías válidas en él son de hoy, 08:00–08:59).</summary>
    public static int CrearHorario(ApiFactory f, string correoDueno, string estado = "Disponible", string cupos = "10") => f.ConDb(db =>
    {
        var h = new Horario
        {
            titulo = "Horario prueba", fechaInicio = DateTime.Today, fechaFin = DateTime.Today,
            horaInicio = DateTime.Today.AddHours(8), horaFin = DateTime.Today.AddHours(9),
            cupos = cupos, espacio = "Sala T", estado = estado, Usuario_idUsuario = Seed.IdDe(db, correoDueno)
        };
        db.Horarios.Add(h);
        db.SaveChanges();
        return h.idHorario;
    });

    public static int CrearTutoria(ApiFactory f, string estado, params string[] correosEstudiantes) => f.ConDb(db =>
    {
        var horarioId = CrearHorario(f, Seed.Docente, "Ocupado");
        var t = new Tutoria
        {
            idioma = "Inglés", nivel = "A1", tema = "Prueba", modalidad = "Virtual", estado = estado,
            fechaHora = DateTime.Now.AddDays(1), Usuario_idUsuario = Seed.IdDe(db, Seed.Tutor), Horario_idHorario = horarioId
        };
        db.Tutorias.Add(t);
        db.SaveChanges();
        foreach (var c in correosEstudiantes)
        {
            db.TutoriaEstudiantes.Add(new TutoriaEstudiante { Tutoria_idTutoria = t.idTutoria, Usuario_idUsuario = Seed.IdDe(db, c) });
        }
        db.SaveChanges();
        return t.idTutoria;
    });

    /// <summary>Tutoría en un horario concreto, asignada a <paramref name="correoAsignado"/>.</summary>
    public static int CrearTutoriaEn(ApiFactory f, int horarioId, string correoAsignado, string estado = "Pendiente", params string[] correosEstudiantes) => f.ConDb(db =>
    {
        var t = new Tutoria
        {
            idioma = "Inglés", nivel = "A2", tema = "Prueba", modalidad = "Presencial", estado = estado,
            fechaHora = DateTime.Today.AddHours(8), Usuario_idUsuario = Seed.IdDe(db, correoAsignado), Horario_idHorario = horarioId
        };
        db.Tutorias.Add(t);
        db.SaveChanges();
        foreach (var c in correosEstudiantes)
        {
            db.TutoriaEstudiantes.Add(new TutoriaEstudiante { Tutoria_idTutoria = t.idTutoria, Usuario_idUsuario = Seed.IdDe(db, c) });
        }
        db.SaveChanges();
        return t.idTutoria;
    });

    /// <summary>Crea un usuario con la contraseña de pruebas (o devuelve el existente).</summary>
    public static int CrearUsuario(ApiFactory f, string correo, int rolId) => f.ConDb(db =>
    {
        var existente = db.Usuarios.FirstOrDefault(u => u.correo == correo);
        if (existente != null) return existente.idUsuario;
        var u = new Usuario
        {
            nombres = correo.Split('@')[0], apellidos = "Prueba", correo = correo,
            contrasenaHash = BCrypt.Net.BCrypt.HashPassword(ApiFactory.Password, 4),
            fechaRegistro = DateTime.Now, Rol_idRol = rolId
        };
        db.Usuarios.Add(u);
        db.SaveChanges();
        return u.idUsuario;
    });

    public static async Task<JsonNode> JsonAsync(this HttpResponseMessage resp)
    {
        var texto = await resp.Content.ReadAsStringAsync();
        return JsonNode.Parse(texto) ?? throw new InvalidOperationException("Respuesta vacía");
    }

    public static string? Str(this JsonNode node, string prop) => node[prop]?.GetValue<string>();
}
