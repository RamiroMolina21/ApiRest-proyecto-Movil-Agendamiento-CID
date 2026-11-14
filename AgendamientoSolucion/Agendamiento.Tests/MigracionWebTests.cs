using Agendamiento.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Agendamiento.Tests;

/// <summary>
/// Requisitos R1–R11 de la migración MAUI → web (docs/api, migracion-maui-web.md §3).
/// Escenario base: el Docente es dueño del horario y asigna la tutoría al Tutor.
/// </summary>
public class MigracionWebTests : IClassFixture<ApiFactory>
{
    private const string Docente2 = "docente2@test.com";
    private const string Tutor2 = "tutor2@test.com";

    private readonly ApiFactory _f;

    public MigracionWebTests(ApiFactory factory)
    {
        _f = factory;
        Datos.CrearUsuario(_f, Docente2, Seed.RolDocente);
        Datos.CrearUsuario(_f, Tutor2, Seed.RolTutor);
    }

    private int Id(string correo) => _f.ConDb(db => Seed.IdDe(db, correo));

    private static object NuevaTutoria(int usuarioId, int horarioId, DateTime? fecha = null, int[]? estudianteIds = null) => new
    {
        idioma = "Inglés", nivel = "B1", tema = "Web", modalidad = "Virtual", estado = "Pendiente",
        fechaTutoria = fecha ?? DateTime.Today.AddHours(8), usuarioId, horarioId, estudianteIds
    };

    private static async Task<HttpStatusCode> Status(Task<HttpResponseMessage> peticion) => (await peticion).StatusCode;

    // ------------------------------------------------------------------ R2

    [Fact]
    public async Task R2_docente_dueno_del_horario_gestiona_la_tutoria_aunque_la_de_un_tutor()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1); // horario del Docente, asignada al Tutor
        using var docente = await _f.ClienteComoAsync(Seed.Docente);

        Assert.Equal(HttpStatusCode.OK, await Status(docente.GetAsync($"/api/Tutoria/{tutoriaId}")));
        Assert.Equal(HttpStatusCode.OK, await Status(docente.GetAsync($"/api/Tutoria/{tutoriaId}/estudiantes")));
        Assert.Equal(HttpStatusCode.OK, await Status(docente.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes",
            new { estudianteIds = new[] { Id(Seed.Estudiante2) } })));
        Assert.Equal(HttpStatusCode.OK, await Status(docente.DeleteAsync($"/api/Tutoria/{tutoriaId}/estudiantes/{Id(Seed.Estudiante2)}")));
        Assert.Equal(HttpStatusCode.OK, await Status(docente.GetAsync($"/api/Retroalimentacion/tutoria/{tutoriaId}")));
        Assert.Equal(HttpStatusCode.OK, await Status(docente.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null)));
    }

    [Fact]
    public async Task R2_docente_y_tutor_ajenos_reciben_403_en_todas_las_acciones()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1);
        var est2 = Id(Seed.Estudiante2);
        var est1 = Id(Seed.Estudiante1);

        foreach (var correo in new[] { Docente2, Tutor2 })
        {
            using var c = await _f.ClienteComoAsync(correo);
            var urls = new (HttpMethod Metodo, string Url, object? Body)[]
            {
                (HttpMethod.Get, $"/api/Tutoria/{tutoriaId}", null),
                (HttpMethod.Get, $"/api/Tutoria/{tutoriaId}/estudiantes", null),
                (HttpMethod.Post, $"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { est2 } }),
                (HttpMethod.Delete, $"/api/Tutoria/{tutoriaId}/estudiantes/{est1}", null),
                (HttpMethod.Put, $"/api/Tutoria/{tutoriaId}/finalizar", null),
                (HttpMethod.Post, $"/api/Encuesta/enviar/{tutoriaId}", null),
                (HttpMethod.Get, $"/api/Retroalimentacion/tutoria/{tutoriaId}", null),
            };

            foreach (var (metodo, url, body) in urls)
            {
                var req = new HttpRequestMessage(metodo, url);
                if (body != null) req.Content = JsonContent.Create(body);
                var resp = await c.SendAsync(req);
                Assert.True(resp.StatusCode == HttpStatusCode.Forbidden, $"{correo} {metodo} {url} → {(int)resp.StatusCode}");
            }
        }

        // Nada cambió y Admin/Aux siguen sin restricción.
        Assert.Equal("Pendiente", _f.ConDb(db => db.Tutorias.Single(t => t.idTutoria == tutoriaId).estado));
        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);
        Assert.Equal(HttpStatusCode.OK, await Status(aux.GetAsync($"/api/Tutoria/{tutoriaId}")));
        Assert.Equal(HttpStatusCode.OK, await Status(aux.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null)));
    }

    [Fact]
    public async Task R2_tutor_asignado_ve_y_finaliza_pero_no_gestiona_estudiantes()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1);
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);

        Assert.Equal(HttpStatusCode.OK, await Status(tutor.GetAsync($"/api/Tutoria/{tutoriaId}")));
        Assert.Equal(HttpStatusCode.OK, await Status(tutor.GetAsync($"/api/Tutoria/{tutoriaId}/estudiantes")));

        var agregar = await tutor.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { Id(Seed.Estudiante2) } });
        Assert.Equal(HttpStatusCode.Forbidden, agregar.StatusCode);
        Assert.Equal("El Tutor no puede agregar ni quitar estudiantes de una tutoría", (await agregar.JsonAsync()).Str("message"));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(tutor.DeleteAsync($"/api/Tutoria/{tutoriaId}/estudiantes/{Id(Seed.Estudiante1)}")));

        Assert.Equal(HttpStatusCode.OK, await Status(tutor.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null)));
    }

    // ------------------------------------------------------------------ R1 + R7

    [Fact]
    public async Task R1_agendadas_devuelve_las_tutorias_de_mis_horarios_con_filtro_por_grupo_de_estado()
    {
        var horario = Datos.CrearHorario(_f, Docente2, "Ocupado");
        var pendiente = Datos.CrearTutoriaEn(_f, horario, Seed.Tutor, "Pendiente");
        var programada = Datos.CrearTutoriaEn(_f, horario, Seed.Tutor, "Programada");
        var completada = Datos.CrearTutoriaEn(_f, horario, Docente2, "Completada");
        var docente2Id = Id(Docente2);

        using var docente2 = await _f.ClienteComoAsync(Docente2);

        var todas = (await (await docente2.GetAsync($"/api/Tutoria/agendadas/usuario/{docente2Id}")).JsonAsync()).AsArray();
        Assert.Equal(new[] { pendiente, programada, completada }.OrderBy(x => x),
            todas.Select(t => t!["idTutoria"]!.GetValue<int>()).OrderBy(x => x));
        Assert.All(todas, t => Assert.Equal(docente2Id, t!["horarioUsuarioId"]!.GetValue<int>()));

        var pendientes = (await (await docente2.GetAsync($"/api/Tutoria/agendadas/usuario/{docente2Id}?estado=pendiente")).JsonAsync()).AsArray();
        Assert.Equal(new[] { pendiente, programada }.OrderBy(x => x), pendientes.Select(t => t!["idTutoria"]!.GetValue<int>()).OrderBy(x => x));

        var finalizadas = (await (await docente2.GetAsync($"/api/Tutoria/agendadas/usuario/{docente2Id}?estado=Finalizada")).JsonAsync()).AsArray();
        Assert.Equal(completada, finalizadas.Single()!["idTutoria"]!.GetValue<int>());

        // Anti-BOLA: otro Docente no puede pedir las agendadas de Docente2; Admin sí.
        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        Assert.Equal(HttpStatusCode.Forbidden, await Status(docente.GetAsync($"/api/Tutoria/agendadas/usuario/{docente2Id}")));
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.OK, await Status(admin.GetAsync($"/api/Tutoria/agendadas/usuario/{docente2Id}")));
    }

    [Fact]
    public async Task R7_estado_usuario_acepta_sinonimos_sin_distinguir_mayusculas()
    {
        var horario = Datos.CrearHorario(_f, Seed.Docente, "Ocupado");
        var ids = new Dictionary<string, int>
        {
            ["Pendiente"] = Datos.CrearTutoriaEn(_f, horario, Tutor2, "Pendiente"),
            ["Programada"] = Datos.CrearTutoriaEn(_f, horario, Tutor2, "Programada"),
            ["activo"] = Datos.CrearTutoriaEn(_f, horario, Tutor2, "activo"),
            ["Finalizada"] = Datos.CrearTutoriaEn(_f, horario, Tutor2, "Finalizada"),
            ["Completada"] = Datos.CrearTutoriaEn(_f, horario, Tutor2, "Completada"),
            ["Cancelada"] = Datos.CrearTutoriaEn(_f, horario, Tutor2, "Cancelada"),
        };
        var tutor2Id = Id(Tutor2);
        using var tutor2 = await _f.ClienteComoAsync(Tutor2);

        async Task<int[]> Pedir(string estado) =>
            (await (await tutor2.GetAsync($"/api/Tutoria/estado/{estado}/usuario/{tutor2Id}")).JsonAsync()).AsArray()
            .Select(t => t!["idTutoria"]!.GetValue<int>()).OrderBy(x => x).ToArray();

        Assert.Equal(new[] { ids["Pendiente"], ids["Programada"], ids["activo"] }.OrderBy(x => x), await Pedir("pendiente"));
        Assert.Equal(new[] { ids["Pendiente"], ids["Programada"], ids["activo"] }.OrderBy(x => x), await Pedir("Pendiente"));
        Assert.Equal(new[] { ids["Finalizada"], ids["Completada"] }.OrderBy(x => x), await Pedir("FINALIZADA"));
        Assert.Equal(new[] { ids["Cancelada"] }, await Pedir("cancelada"));
    }

    // ------------------------------------------------------------------ R4

    [Fact]
    public async Task R4_validaciones_de_crear_tutoria()
    {
        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var tutorId = Id(Seed.Tutor);

        var propio = Datos.CrearHorario(_f, Seed.Docente);
        var ajeno = Datos.CrearHorario(_f, Docente2);
        var ocupado = Datos.CrearHorario(_f, Seed.Docente, "Ocupado");

        // Tutor no agenda.
        Assert.Equal(HttpStatusCode.Forbidden, await Status(tutor.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, propio))));
        // Docente solo en sus horarios.
        Assert.Equal(HttpStatusCode.Forbidden, await Status(docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, ajeno))));
        // Horario no disponible.
        var conflicto = await docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, ocupado));
        Assert.Equal(HttpStatusCode.Conflict, conflicto.StatusCode);
        Assert.Equal("El horario ya no está disponible", (await conflicto.JsonAsync()).Str("message"));
        // Asignado debe ser Docente o Tutor (la web de gestión enviaba un estudiante).
        var noDocente = await docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(Id(Seed.Estudiante1), propio));
        Assert.Equal(HttpStatusCode.BadRequest, noDocente.StatusCode);
        Assert.Equal("El usuario asignado debe ser Docente o Tutor", (await noDocente.JsonAsync()).Str("message"));
        // Fecha fuera del horario y hora fuera de la franja.
        Assert.Equal(HttpStatusCode.BadRequest, await Status(docente.PostAsJsonAsync("/api/Tutoria",
            NuevaTutoria(tutorId, propio, DateTime.Today.AddDays(2).AddHours(8)))));
        Assert.Equal(HttpStatusCode.BadRequest, await Status(docente.PostAsJsonAsync("/api/Tutoria",
            NuevaTutoria(tutorId, propio, DateTime.Today.AddHours(10)))));

        // Ningún intento fallido tocó el horario.
        Assert.Equal("Disponible", _f.ConDb(db => db.Horarios.Single(h => h.idHorario == propio).estado));

        // Sin hora (00:00) → se toma la hora de inicio del horario. El horario pasa a Ocupado.
        var ok = await docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, propio, DateTime.Today));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var creada = await ok.JsonAsync();
        Assert.Equal(DateTime.Today.AddHours(8), creada["fechaTutoria"]!.GetValue<DateTime>());
        Assert.Equal("Ocupado", _f.ConDb(db => db.Horarios.Single(h => h.idHorario == propio).estado));

        // El mismo horario ya no admite otra tutoría (evita doble reserva).
        Assert.Equal(HttpStatusCode.Conflict, await Status(docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, propio))));

        // Admin/Aux: cualquier horario disponible (sin restricción de propiedad).
        Assert.Equal(HttpStatusCode.Created, await Status(admin.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, ajeno))));
    }

    // ------------------------------------------------------------------ R5

    [Fact]
    public async Task R5_cupos_limitan_los_estudiantes_de_la_tutoria_todo_o_nada()
    {
        var e3 = Datos.CrearUsuario(_f, "cupo3@test.com", Seed.RolEstudiante);
        var e1 = Id(Seed.Estudiante1);
        var e2 = Id(Seed.Estudiante2);
        var horario = Datos.CrearHorario(_f, Seed.Docente, "Ocupado", cupos: "2");
        var tutoriaId = Datos.CrearTutoriaEn(_f, horario, Seed.Tutor);
        using var docente = await _f.ClienteComoAsync(Seed.Docente);

        var demasiados = await docente.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { e1, e2, e3 } });
        Assert.Equal(HttpStatusCode.Conflict, demasiados.StatusCode);
        Assert.StartsWith("Cupo completo: 0 de 2", (await demasiados.JsonAsync()).Str("message"));
        Assert.Equal(0, _f.ConDb(db => db.TutoriaEstudiantes.Count(te => te.Tutoria_idTutoria == tutoriaId)));

        Assert.Equal(HttpStatusCode.OK, await Status(docente.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { e1, e2 } })));

        var lleno = await docente.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { e3 } });
        Assert.Equal(HttpStatusCode.Conflict, lleno.StatusCode);
        Assert.StartsWith("Cupo completo: 2 de 2", (await lleno.JsonAsync()).Str("message"));

        // Datos antiguos con cupos no numéricos: sin límite.
        var legado = Datos.CrearTutoriaEn(_f, Datos.CrearHorario(_f, Seed.Docente, "Ocupado", cupos: "string"), Seed.Tutor);
        Assert.Equal(HttpStatusCode.OK, await Status(docente.PostAsJsonAsync($"/api/Tutoria/{legado}/estudiantes", new { estudianteIds = new[] { e1, e2, e3 } })));
    }

    [Theory]
    [InlineData("abc", HttpStatusCode.BadRequest)]
    [InlineData("0", HttpStatusCode.BadRequest)]
    [InlineData("-3", HttpStatusCode.BadRequest)]
    [InlineData("2.5", HttpStatusCode.BadRequest)]
    [InlineData(" 05 ", HttpStatusCode.Created)]
    public async Task R5_post_horario_valida_que_cupos_sea_entero_positivo(string cupos, HttpStatusCode esperado)
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.PostAsJsonAsync("/api/Horario", new
        {
            titulo = "Cupos", fechaInicio = DateTime.Today, fechaFin = DateTime.Today,
            horaInicio = DateTime.Today.AddHours(10), horaFin = DateTime.Today.AddHours(11),
            cupos, espacio = "Sala", estado = "Disponible", usuarioId = Id(Seed.Docente)
        });

        Assert.Equal(esperado, resp.StatusCode);
        if (esperado == HttpStatusCode.Created)
            Assert.Equal("5", (await resp.JsonAsync()).Str("cupos"));
    }

    // ------------------------------------------------------------------ R6

    [Fact]
    public async Task R6_tutoria_response_incluye_datos_del_horario_y_su_dueno()
    {
        var horario = Datos.CrearHorario(_f, Seed.Docente, "Ocupado", cupos: "7");
        var tutoriaId = Datos.CrearTutoriaEn(_f, horario, Seed.Tutor);
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);

        var t = await (await tutor.GetAsync($"/api/Tutoria/{tutoriaId}")).JsonAsync();
        Assert.Equal(Id(Seed.Docente), t["horarioUsuarioId"]!.GetValue<int>());
        Assert.Equal("docente", t.Str("horarioUsuarioNombre"));
        Assert.Equal("Prueba", t.Str("horarioUsuarioApellidos"));
        Assert.Equal("Horario prueba", t.Str("horarioTitulo"));
        Assert.Equal(7, t["horarioCupos"]!.GetValue<int>());

        var legado = Datos.CrearTutoriaEn(_f, Datos.CrearHorario(_f, Seed.Docente, "Ocupado", cupos: "string"), Seed.Tutor);
        var l = await (await tutor.GetAsync($"/api/Tutoria/{legado}")).JsonAsync();
        Assert.Null(l["horarioCupos"]);
    }

    // ------------------------------------------------------------------ R8

    [Fact]
    public async Task R8_finalizar_con_calificacion_crea_la_retroalimentacion_del_token_atomicamente()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1);
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);

        var fin = await tutor.PutAsJsonAsync($"/api/Tutoria/{tutoriaId}/finalizar", new { calificacion = 4.5m, comentario = "  Muy bien  " });
        Assert.Equal(HttpStatusCode.OK, fin.StatusCode);
        Assert.Equal("Finalizada", (await fin.JsonAsync()).Str("estado"));

        var retro = _f.ConDb(db => db.Retroalimentaciones.Single(r => r.Tutoria_idTutoria == tutoriaId));
        Assert.Equal(Id(Seed.Tutor), retro.Usuario_idUsuario);
        Assert.Equal(4.5m, retro.calificacion);
        Assert.Equal("Muy bien", retro.comentario);
        Assert.Equal(1, _f.ConDb(db => db.EncuestasSatisfaccion.Count(e => e.Tutoria_idTutoria == tutoriaId)));

        // Reintento: ya finalizada → 400 y no se duplica la retroalimentación.
        Assert.Equal(HttpStatusCode.BadRequest, await Status(tutor.PutAsJsonAsync($"/api/Tutoria/{tutoriaId}/finalizar", new { calificacion = 4.5m })));
        Assert.Equal(1, _f.ConDb(db => db.Retroalimentaciones.Count(r => r.Tutoria_idTutoria == tutoriaId)));
    }

    [Fact]
    public async Task R8_cuerpo_invalido_o_retro_duplicada_no_finaliza()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente");
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);

        Assert.Equal(HttpStatusCode.BadRequest, await Status(tutor.PutAsJsonAsync($"/api/Tutoria/{tutoriaId}/finalizar", new { calificacion = 7 })));
        Assert.Equal(HttpStatusCode.BadRequest, await Status(tutor.PutAsJsonAsync($"/api/Tutoria/{tutoriaId}/finalizar", new { comentario = "sin nota" })));

        // MAUI: primero POST Retroalimentacion; luego finalizar CON cuerpo → 409 y la tutoría sigue pendiente (rollback).
        Assert.Equal(HttpStatusCode.Created, await Status(tutor.PostAsJsonAsync("/api/Retroalimentacion",
            new { comentario = "", calificacion = 5, tutoriaId, usuarioId = Id(Seed.Tutor) })));
        Assert.Equal(HttpStatusCode.Conflict, await Status(tutor.PutAsJsonAsync($"/api/Tutoria/{tutoriaId}/finalizar", new { calificacion = 3 })));
        Assert.Equal("Pendiente", _f.ConDb(db => db.Tutorias.Single(t => t.idTutoria == tutoriaId).estado));

        // Sin cuerpo (compatibilidad MAUI) sí finaliza.
        Assert.Equal(HttpStatusCode.OK, await Status(tutor.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null)));
    }

    // ------------------------------------------------------------------ R9

    [Fact]
    public async Task R9_crear_con_estudiantes_es_atomico_y_respeta_cupos()
    {
        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        var tutorId = Id(Seed.Tutor);
        var e1 = Id(Seed.Estudiante1);
        var e2 = Id(Seed.Estudiante2);

        var horario = Datos.CrearHorario(_f, Seed.Docente, cupos: "1");
        var tutoriasAntes = _f.ConDb(db => db.Tutorias.Count());

        // Supera el cupo → 409, no se crea nada y el horario sigue disponible.
        var excede = await docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, horario, estudianteIds: new[] { e1, e2 }));
        Assert.Equal(HttpStatusCode.Conflict, excede.StatusCode);
        // Estudiante inexistente → 404, tampoco se crea nada.
        Assert.Equal(HttpStatusCode.NotFound, await Status(docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, horario, estudianteIds: new[] { 999999 }))));
        Assert.Equal(tutoriasAntes, _f.ConDb(db => db.Tutorias.Count()));
        Assert.Equal("Disponible", _f.ConDb(db => db.Horarios.Single(h => h.idHorario == horario).estado));

        var ok = await docente.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(tutorId, horario, estudianteIds: new[] { e1, e1 }));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var json = await ok.JsonAsync();
        Assert.Equal(e1, json["estudiantes"]!.AsArray().Single()!["idUsuario"]!.GetValue<int>());
        Assert.Equal(1, json["horarioCupos"]!.GetValue<int>());
    }

    // ------------------------------------------------------------------ R3 + R10

    [Fact]
    public async Task R3_retroalimentacion_autor_del_token_participante_y_sin_duplicados()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente");
        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        using var docente2 = await _f.ClienteComoAsync(Docente2);
        object Retro(int usuarioId) => new { comentario = "ok", calificacion = 4, tutoriaId, usuarioId };

        // Firmar por otro usuario → 403.
        var suplanta = await docente.PostAsJsonAsync("/api/Retroalimentacion", Retro(Id(Seed.Tutor)));
        Assert.Equal(HttpStatusCode.Forbidden, suplanta.StatusCode);
        Assert.Equal("Solo puede registrar retroalimentaciones a su propio nombre", (await suplanta.JsonAsync()).Str("message"));
        // No participa → 403.
        Assert.Equal(HttpStatusCode.Forbidden, await Status(docente2.PostAsJsonAsync("/api/Retroalimentacion", Retro(Id(Docente2)))));
        // Dueño del horario a su nombre → 201; segunda vez → 409.
        Assert.Equal(HttpStatusCode.Created, await Status(docente.PostAsJsonAsync("/api/Retroalimentacion", Retro(Id(Seed.Docente)))));
        Assert.Equal(HttpStatusCode.Conflict, await Status(docente.PostAsJsonAsync("/api/Retroalimentacion", Retro(Id(Seed.Docente)))));

        // R10: el participante lee las retroalimentaciones de la tutoría.
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);
        var lista = (await (await tutor.GetAsync($"/api/Retroalimentacion/tutoria/{tutoriaId}")).JsonAsync()).AsArray();
        Assert.Single(lista);
        // Admin sigue igual: tutoría inexistente → lista vacía.
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Empty((await (await admin.GetAsync("/api/Retroalimentacion/tutoria/987654")).JsonAsync()).AsArray());
    }

    // ------------------------------------------------------------------ R11

    [Fact]
    public async Task R11_cors_permite_el_preview_de_vite()
    {
        using var c = _f.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/Auth/me");
        req.Headers.Add("Origin", "http://localhost:4173");
        req.Headers.Add("Access-Control-Request-Method", "GET");
        req.Headers.Add("Access-Control-Request-Headers", "authorization");

        var resp = await c.SendAsync(req);
        Assert.Equal("http://localhost:4173", resp.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task R11_me_da_401_si_el_usuario_del_token_fue_borrado()
    {
        var correo = "borrable@test.com";
        var id = Datos.CrearUsuario(_f, correo, Seed.RolDocente);
        using var borrable = await _f.ClienteComoAsync(correo);
        Assert.Equal(HttpStatusCode.OK, await Status(borrable.GetAsync("/api/Auth/me")));

        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.OK, await Status(admin.DeleteAsync($"/api/Usuario/{id}")));

        var me = await borrable.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("El usuario del token ya no existe", (await me.JsonAsync()).Str("message"));
    }
}

/// <summary>Fábrica propia: modifica el SMTP falso (FallarPara) sin afectar a otras clases de prueba.</summary>
public class NotificacionPorDestinatarioTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public NotificacionPorDestinatarioTests(ApiFactory factory) => _f = factory;

    [Fact]
    public async Task Un_fallo_de_correo_con_un_destinatario_no_corta_el_envio_a_los_demas()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1, Seed.Estudiante2);
        var t = _f.ConDb(db => db.Tutorias.Single(x => x.idTutoria == tutoriaId));
        var est1 = _f.ConDb(db => Seed.IdDe(db, Seed.Estudiante1));
        var est2 = _f.ConDb(db => Seed.IdDe(db, Seed.Estudiante2));
        _f.Correo.FallarPara[Seed.Estudiante1] = true;

        try
        {
            using var admin = await _f.ClienteComoAsync(Seed.Admin);
            // PUT Tutoria/{id} dispara la notificación de cambio a todos los estudiantes.
            var resp = await admin.PutAsJsonAsync($"/api/Tutoria/{tutoriaId}", new
            {
                idioma = t.idioma, nivel = t.nivel, tema = "Tema cambiado", modalidad = t.modalidad, estado = t.estado,
                fechaTutoria = t.fechaHora, usuarioId = t.Usuario_idUsuario, horarioId = t.Horario_idHorario
            });
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
        finally
        {
            _f.Correo.FallarPara.Clear();
        }

        Assert.Contains(_f.Correo.Enviados, c => c.Destinatario == Seed.Estudiante2 && c.Asunto.StartsWith("Cambio en Tutoría"));
        // La fila Notificacion solo se guarda si el correo salió (semántica sin cambios).
        Assert.True(_f.ConDb(db => db.Notificaciones.Any(n => n.Usuario_idUsuario == est2 && n.asunto.StartsWith("Cambio en Tutoría"))));
        Assert.False(_f.ConDb(db => db.Notificaciones.Any(n => n.Usuario_idUsuario == est1 && n.asunto.StartsWith("Cambio en Tutoría"))));
    }
}
