using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Entidades.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace Agendamiento.Tests;

/// <summary>Regresiones de los bugs corregidos (A1–A6, M1, M2, M6, M7) y endpoints públicos de encuesta.</summary>
public class CorreccionesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public CorreccionesTests(ApiFactory factory) => _f = factory;

    [Fact]
    public async Task A1_eliminar_tutoria_con_encuestas_ya_no_da_500()
    {
        // La prueba solo tiene sentido si la BD de pruebas aplica las FK (como SQL Server).
        var fkActivas = _f.ConDb(db =>
        {
            var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys;";
            return Convert.ToInt64(cmd.ExecuteScalar());
        });
        Assert.Equal(1, fkActivas);

        var tutoriaId = Datos.CrearTutoria(_f, "Finalizada", Seed.Estudiante1);
        _f.ConDb(db =>
        {
            db.EncuestasSatisfaccion.Add(new EncuestaSatisfaccion
            {
                tokenRespuesta = Guid.NewGuid().ToString("N"), Tutoria_idTutoria = tutoriaId,
                estudianteCorreo = Seed.Estudiante1, estudianteNombre = "est1"
            });
            db.Retroalimentaciones.Add(new Retroalimentacion
            {
                comentario = "ok", calificacion = 5, fecha = DateTime.Now, Tutoria_idTutoria = tutoriaId,
                Usuario_idUsuario = Seed.IdDe(db, Seed.Tutor)
            });
            return db.SaveChanges();
        });

        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.DeleteAsync($"/api/Tutoria/{tutoriaId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.False(_f.ConDb(db => db.Tutorias.Any(t => t.idTutoria == tutoriaId)));
        Assert.False(_f.ConDb(db => db.EncuestasSatisfaccion.Any(e => e.Tutoria_idTutoria == tutoriaId)));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/Tutoria/{tutoriaId}")).StatusCode);
    }

    [Fact]
    public async Task A2_finalizar_y_luego_enviar_encuestas_no_duplica()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1, Seed.Estudiante2);
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);

        var fin = await tutor.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null);
        Assert.Equal(HttpStatusCode.OK, fin.StatusCode);
        Assert.Equal("Finalizada", (await fin.JsonAsync()).Str("estado"));

        // Lo que hace MAUI justo después de finalizar:
        var enviar = await tutor.PostAsync($"/api/Encuesta/enviar/{tutoriaId}", null);
        Assert.Equal(HttpStatusCode.OK, enviar.StatusCode);
        var json = await enviar.JsonAsync();
        Assert.Equal(0, json["enviadas"]!.GetValue<int>());
        Assert.Equal(2, json["yaExistentes"]!.GetValue<int>());

        Assert.Equal(2, _f.ConDb(db => db.EncuestasSatisfaccion.Count(e => e.Tutoria_idTutoria == tutoriaId)));

        // Finalizar dos veces → 400 claro
        Assert.Equal(HttpStatusCode.BadRequest, (await tutor.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null)).StatusCode);
    }

    [Fact]
    public async Task A3_endpoints_publicos_de_encuesta_sin_token_y_comentario_null()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Finalizada", Seed.Estudiante1);
        var token = Guid.NewGuid().ToString("N");
        _f.ConDb(db =>
        {
            db.EncuestasSatisfaccion.Add(new EncuestaSatisfaccion
            {
                tokenRespuesta = token, Tutoria_idTutoria = tutoriaId, estudianteCorreo = Seed.Estudiante1, estudianteNombre = "est1"
            });
            return db.SaveChanges();
        });

        using var anon = _f.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/api/Encuesta/verificar/token-inexistente")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/api/Encuesta/verificar/{token}")).StatusCode);

        // Un token basura en el navegador no debe romper la página pública.
        using var conTokenBasura = _f.CreateClient();
        conTokenBasura.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "no.es.un.jwt");
        Assert.Equal(HttpStatusCode.OK, (await conTokenBasura.GetAsync($"/api/Encuesta/verificar/{token}")).StatusCode);

        var resp = await anon.PostAsJsonAsync("/api/Encuesta/responder", new { token, calificacion = 4, comentario = (string?)null });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var otraVez = await anon.PostAsJsonAsync("/api/Encuesta/responder", new { token, calificacion = 4 });
        Assert.Equal(HttpStatusCode.BadRequest, otraVez.StatusCode);
        Assert.Equal("Esta encuesta ya fue respondida", (await otraVez.JsonAsync()).Str("message"));
    }

    [Fact]
    public async Task A3_retroalimentacion_con_comentario_vacio_como_envia_MAUI()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente");
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);
        var tutorId = _f.ConDb(db => Seed.IdDe(db, Seed.Tutor));

        var resp = await tutor.PostAsJsonAsync("/api/Retroalimentacion",
            new { comentario = "", calificacion = 5, tutoriaId, usuarioId = tutorId });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await resp.JsonAsync();
        Assert.Equal(tutoriaId, json["tutoriaId"]!.GetValue<int>());
    }

    [Fact]
    public async Task A4_A5_metricas_toleran_sinonimos_y_GET_no_escribe_en_Informe()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var antes = (await (await admin.GetAsync("/api/Metricas/generales")).JsonAsync());

        Datos.CrearTutoria(_f, "Finalizada");
        Datos.CrearTutoria(_f, "Completada");
        Datos.CrearTutoria(_f, "Pendiente");
        Datos.CrearTutoria(_f, "Programada");
        Datos.CrearHorario(_f, Seed.Docente, "Libre");

        var despues = await (await admin.GetAsync("/api/Metricas/generales")).JsonAsync();
        int Delta(string campo) => despues[campo]!.GetValue<int>() - antes[campo]!.GetValue<int>();

        Assert.Equal(2, Delta("tutoriasCompletadas"));
        Assert.Equal(2, Delta("tutoriasProgramadas"));
        Assert.Equal(1, Delta("horariosDisponibles"));

        var informesAntes = _f.ConDb(db => db.Informes.Count());
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/Metricas/tutorias")).StatusCode);
        Assert.Equal(informesAntes, _f.ConDb(db => db.Informes.Count()));
    }

    [Fact]
    public async Task A4_horarios_disponibles_incluye_Libre()
    {
        var libreId = Datos.CrearHorario(_f, Seed.Docente, "Libre");
        using var docente = await _f.ClienteComoAsync(Seed.Docente);

        var lista = (await (await docente.GetAsync("/api/Horario/disponibles")).JsonAsync()).AsArray();
        var item = lista.Single(h => h!["idHorario"]!.GetValue<int>() == libreId)!;
        Assert.Equal(_f.ConDb(db => Seed.IdDe(db, Seed.Docente)), item["usuarioId"]!.GetValue<int>());
    }

    [Fact]
    public async Task M1_put_usuario_sin_contrasena_conserva_la_actual_y_detecta_correo_duplicado()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var creado = await (await admin.PostAsJsonAsync("/api/Usuario",
            AutorizacionTests.NuevoUsuario("m1@test.com", Seed.RolDocente))).JsonAsync();
        var id = creado["idUsuario"]!.GetValue<int>();

        var put = await admin.PutAsJsonAsync($"/api/Usuario/{id}",
            new { nombres = "Cambiado", apellidos = "X", correo = "m1@test.com", contrasena = "", rolId = Seed.RolDocente });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Cambiado", (await put.JsonAsync()).Str("nombres"));

        using var anon = _f.CreateClient();
        var login = await anon.PostAsJsonAsync("/api/Auth/login", new { correo = "m1@test.com", contrasena = "Clave1234" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var duplicado = await admin.PutAsJsonAsync($"/api/Usuario/{id}",
            new { nombres = "Cambiado", apellidos = "X", correo = Seed.Docente, rolId = Seed.RolDocente });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var corta = await admin.PutAsJsonAsync($"/api/Usuario/{id}",
            new { nombres = "Cambiado", apellidos = "X", correo = "m1@test.com", contrasena = "123", rolId = Seed.RolDocente });
        Assert.Equal(HttpStatusCode.BadRequest, corta.StatusCode);
    }

    [Fact]
    public async Task M2_agregar_estudiantes_es_todo_o_nada()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente");
        var est1 = _f.ConDb(db => Seed.IdDe(db, Seed.Estudiante1));
        using var docente = await _f.ClienteComoAsync(Seed.Docente);

        var resp = await docente.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { est1, 999999 } });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal(0, _f.ConDb(db => db.TutoriaEstudiantes.Count(te => te.Tutoria_idTutoria == tutoriaId)));

        var ok = await docente.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { est1 } });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(1, _f.ConDb(db => db.TutoriaEstudiantes.Count(te => te.Tutoria_idTutoria == tutoriaId)));
    }

    [Fact]
    public async Task M6_respuestas_incluyen_ids_aditivos()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1);
        using var admin = await _f.ClienteComoAsync(Seed.Admin);

        var t = await (await admin.GetAsync($"/api/Tutoria/{tutoriaId}")).JsonAsync();
        var esperado = _f.ConDb(db => db.Tutorias.Single(x => x.idTutoria == tutoriaId));
        Assert.Equal(esperado.Usuario_idUsuario, t["usuarioId"]!.GetValue<int>());
        Assert.Equal(esperado.Horario_idHorario, t["horarioId"]!.GetValue<int>());
        Assert.Equal(Seed.RolEstudiante, t["estudiantes"]![0]!["rolId"]!.GetValue<int>());

        var usuarios = (await (await admin.GetAsync("/api/Usuario")).JsonAsync()).AsArray();
        Assert.All(usuarios, u => Assert.True(u!["rolId"]!.GetValue<int>() > 0));
    }

    [Fact]
    public async Task M7_enlace_de_encuesta_usa_la_url_configurada()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante2);
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);

        Assert.Equal(HttpStatusCode.OK, (await tutor.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null)).StatusCode);

        var token = _f.ConDb(db => db.EncuestasSatisfaccion.Single(e => e.Tutoria_idTutoria == tutoriaId).tokenRespuesta);
        Assert.Contains(_f.Correo.Enviados, c =>
            c.Destinatario == Seed.Estudiante2 && c.Html.Contains($"{ApiFactory.UrlEncuestaDePrueba}?token={token}"));
        Assert.DoesNotContain(_f.Correo.Enviados, c => c.Html.Contains("http://localhost:5173/encuesta"));
    }

    [Fact]
    public async Task Errores_de_negocio_salen_como_problem_details_con_message()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.GetAsync("/api/Tutoria/987654");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        var json = await resp.JsonAsync();
        Assert.Equal("Tutoría no encontrada", json.Str("message"));
        Assert.Equal("Tutoría no encontrada", json.Str("detail"));
        Assert.Equal(404, json["status"]!.GetValue<int>());
    }
}
