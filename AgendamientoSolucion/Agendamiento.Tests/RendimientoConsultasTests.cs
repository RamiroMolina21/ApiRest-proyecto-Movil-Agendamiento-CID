using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Entidades.Entities;
using System.Net;

namespace Agendamiento.Tests;

/// <summary>
/// Guardas contra el N+1: el número de consultas SQL por endpoint debe ser CONSTANTE (no crecer con las filas).
/// Con 30 tutorías × 2 estudiantes, 30 retroalimentaciones, notificaciones e informes, antes del cambio
/// GET /Tutoria hacía 31 consultas, /Retroalimentacion 61, /Notificacion 61+ y /Metricas/tutorias ~150.
/// </summary>
public class RendimientoConsultasTests : IClassFixture<ApiFactory>
{
    private const int N = 30;
    private readonly ApiFactory _f;

    public RendimientoConsultasTests(ApiFactory factory)
    {
        _f = factory;
        Sembrar();
    }

    private void Sembrar() => _f.ConDb(db =>
    {
        if (db.Tutorias.Any(t => t.tema == "Rendimiento")) return 0;
        var docente = Seed.IdDe(db, Seed.Docente);
        var tutor = Seed.IdDe(db, Seed.Tutor);
        var estudiantes = Enumerable.Range(1, 4).Select(i =>
        {
            var u = new Usuario { nombres = $"Rend{i}", apellidos = "Prueba", correo = $"rend{i}@test.com", contrasenaHash = "x", fechaRegistro = DateTime.Now, Rol_idRol = Seed.RolEstudiante };
            db.Usuarios.Add(u);
            return u;
        }).ToList();
        db.SaveChanges();

        for (var i = 0; i < N; i++)
        {
            var h = new Horario
            {
                titulo = $"Rend {i}", fechaInicio = DateTime.Today, fechaFin = DateTime.Today, horaInicio = DateTime.Today.AddHours(8),
                horaFin = DateTime.Today.AddHours(9), cupos = "10", espacio = "Sala R", estado = "Ocupado", Usuario_idUsuario = docente
            };
            db.Horarios.Add(h);
            db.SaveChanges();
            var t = new Tutoria
            {
                idioma = "Inglés", nivel = "B1", tema = "Rendimiento", modalidad = "Virtual", estado = i % 2 == 0 ? "Finalizada" : "Pendiente",
                fechaHora = DateTime.Today.AddHours(8), Usuario_idUsuario = i % 3 == 0 ? tutor : docente, Horario_idHorario = h.idHorario
            };
            db.Tutorias.Add(t);
            db.SaveChanges();
            db.TutoriaEstudiantes.Add(new TutoriaEstudiante { Tutoria_idTutoria = t.idTutoria, Usuario_idUsuario = estudiantes[i % 4].idUsuario });
            db.TutoriaEstudiantes.Add(new TutoriaEstudiante { Tutoria_idTutoria = t.idTutoria, Usuario_idUsuario = estudiantes[(i + 1) % 4].idUsuario });
            db.Retroalimentaciones.Add(new Retroalimentacion { comentario = "r", calificacion = 4, fecha = DateTime.Now, Tutoria_idTutoria = t.idTutoria, Usuario_idUsuario = docente });
            db.Informes.Add(new Informe { descripcion = "i", fechaGeneracion = DateTime.Now, Tutoria_idTutoria = t.idTutoria, Usuario_idUsuario = estudiantes[i % 4].idUsuario });
            db.Notificaciones.Add(new Notificacion { fecha = DateTime.Now, asunto = "a", descripcion = "d", Usuario_idUsuario = estudiantes[i % 4].idUsuario });
            db.EncuestasSatisfaccion.Add(new EncuestaSatisfaccion { Tutoria_idTutoria = t.idTutoria, tokenRespuesta = Guid.NewGuid().ToString("N"), estudianteCorreo = $"rend{i % 4 + 1}@test.com", estudianteNombre = "R", tokenUsado = i % 2 == 0, calificacion = 5 });
        }
        return db.SaveChanges();
    });

    public static IEnumerable<object[]> Endpoints() => new[]
    {
        new object[] { "/api/Tutoria", 3 },
        new object[] { "/api/Tutoria/estado/Finalizada", 3 },
        new object[] { "/api/Tutoria/estado/pendiente/usuario/{docente}", 3 },
        new object[] { "/api/Tutoria/agendadas/usuario/{docente}", 3 },
        new object[] { "/api/Tutoria/calendario/docente?nombre=docente&apellidos=Prueba&correo=docente@test.com", 3 },
        new object[] { "/api/Tutoria/calendario/estudiante?nombre=Rend1&apellidos=Prueba&correo=rend1@test.com", 4 },
        new object[] { "/api/Tutoria/calendario/idioma-nivel?idioma=Ingl%C3%A9s&nivel=B1", 3 },
        new object[] { "/api/Retroalimentacion", 1 },
        new object[] { "/api/Retroalimentacion/usuario/{docente}", 1 },
        new object[] { "/api/Horario", 1 },
        new object[] { "/api/Horario/usuario/{docente}", 1 },
        new object[] { "/api/Usuario", 1 },
        new object[] { "/api/Usuario?page=1&pageSize=50", 2 },
        new object[] { "/api/Notificacion", 1 },
        new object[] { "/api/Encuesta/pendientes", 2 },
        new object[] { "/api/Encuesta/estadisticas", 1 },
        new object[] { "/api/Metricas/generales", 4 },
        new object[] { "/api/Metricas/tutorias", 3 },
        new object[] { "/api/Atencion/atendidos", 1 },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Numero_de_consultas_constante(string url, int maximo)
    {
        url = url.Replace("{docente}", _f.ConDb(db => Seed.IdDe(db, Seed.Docente)).ToString());
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(url)).StatusCode); // calienta la caché de estado de cuenta

        _f.Consultas.Reiniciar();
        var resp = await admin.GetAsync(url);
        var consultas = _f.Consultas.Total;

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True(consultas <= maximo, $"{url}: {consultas} consultas SQL (máximo {maximo}). ¿Volvió el N+1?");
    }

    [Fact]
    public async Task Las_listas_traen_los_datos_relacionados_completos()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var tutorias = (await (await admin.GetAsync("/api/Tutoria")).JsonAsync()).AsArray()
            .Where(t => t!["tema"]!.GetValue<string>() == "Rendimiento").ToList();
        Assert.Equal(N, tutorias.Count);
        Assert.All(tutorias, t => Assert.Equal(2, t!["estudiantes"]!.AsArray().Count));
        // Estudiantes en orden de idUsuario (el orden que ya devolvía SQL Server).
        Assert.All(tutorias, t =>
        {
            var ids = t!["estudiantes"]!.AsArray().Select(e => e!["idUsuario"]!.GetValue<int>()).ToList();
            Assert.Equal(ids.OrderBy(x => x), ids);
        });

        var metricas = (await (await admin.GetAsync("/api/Metricas/tutorias")).JsonAsync()).AsArray()
            .Where(m => m!["tema"]!.GetValue<string>() == "Rendimiento").ToList();
        Assert.All(metricas, m => Assert.Equal(1, m!["numeroAsistencias"]!.GetValue<int>()));

        var retros = (await (await admin.GetAsync("/api/Retroalimentacion")).JsonAsync()).AsArray();
        Assert.All(retros.Where(r => r!["comentario"]!.GetValue<string>() == "r"), r =>
        {
            Assert.Equal("docente", r!["usuarioNombre"]!.GetValue<string>());
            Assert.Equal("Rendimiento", r!["tutoriaTema"]!.GetValue<string>());
        });

        var est = await (await admin.GetAsync("/api/Encuesta/estadisticas")).JsonAsync();
        Assert.True(est["totalEnviadas"]!.GetValue<int>() >= N);
    }
}
