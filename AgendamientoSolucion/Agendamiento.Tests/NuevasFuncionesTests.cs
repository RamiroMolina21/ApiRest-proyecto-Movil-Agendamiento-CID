using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Logica.Utils;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Agendamiento.Tests;

/// <summary>
/// 1) Título de horario opcional · 2) Paginación/búsqueda de usuarios · A) Cuenta habilitada (activo) ·
/// B) Asuntos atendidos del dashboard.
/// </summary>
public class NuevasFuncionesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public NuevasFuncionesTests(ApiFactory factory)
    {
        _f = factory;
    }

    private int Id(string correo) => _f.ConDb(db => Seed.IdDe(db, correo));

    private static readonly DateTime Dia = new(2030, 3, 15);

    private object NuevoHorario(string? titulo, string espacio = "Sala 3", int? usuarioId = null) => new
    {
        titulo,
        fechaInicio = Dia,
        fechaFin = Dia,
        horaInicio = Dia.AddHours(8),
        horaFin = Dia.AddHours(9).AddMinutes(30),
        cupos = "5",
        espacio,
        estado = "Disponible",
        usuarioId = usuarioId ?? Id(Seed.Docente)
    };

    // ------------------------------------------------------------------ 1) Título de horario

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Horario_sin_titulo_recibe_uno_generado(string? titulo)
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.PostAsJsonAsync("/api/Horario", NuevoHorario(titulo));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await resp.JsonAsync();
        Assert.Equal("Sala 3 · 15/03/2030 08:00–09:30", json.Str("titulo"));
    }

    [Fact]
    public async Task Horario_omitiendo_la_propiedad_titulo_recibe_uno_generado()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.PostAsJsonAsync("/api/Horario", new
        {
            fechaInicio = Dia, fechaFin = Dia.AddDays(2), horaInicio = Dia.AddHours(14), horaFin = Dia.AddHours(16),
            cupos = "5", espacio = "Lab", estado = "Disponible", usuarioId = Id(Seed.Docente)
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.Equal("Lab · 15/03/2030–17/03/2030 14:00–16:00", (await resp.JsonAsync()).Str("titulo"));
    }

    [Fact]
    public async Task Horario_con_titulo_se_respeta()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.PostAsJsonAsync("/api/Horario", NuevoHorario("  [PRUEBA] Mi horario  "));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.Equal("[PRUEBA] Mi horario", (await resp.JsonAsync()).Str("titulo"));
    }

    [Fact]
    public async Task Put_sin_titulo_regenera_el_generado_y_conserva_el_personalizado()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);

        var generado = await (await admin.PostAsJsonAsync("/api/Horario", NuevoHorario(null))).JsonAsync();
        var idGenerado = generado["idHorario"]!.GetValue<int>();
        var put1 = await admin.PutAsJsonAsync($"/api/Horario/{idGenerado}", NuevoHorario("", espacio: "Sala 9"));
        Assert.Equal(HttpStatusCode.OK, put1.StatusCode);
        Assert.Equal("Sala 9 · 15/03/2030 08:00–09:30", (await put1.JsonAsync()).Str("titulo"));

        var personalizado = await (await admin.PostAsJsonAsync("/api/Horario", NuevoHorario("Inglés B1"))).JsonAsync();
        var idPersonalizado = personalizado["idHorario"]!.GetValue<int>();
        var put2 = await admin.PutAsJsonAsync($"/api/Horario/{idPersonalizado}", NuevoHorario(null, espacio: "Sala 9"));
        Assert.Equal("Inglés B1", (await put2.JsonAsync()).Str("titulo"));

        var put3 = await admin.PutAsJsonAsync($"/api/Horario/{idPersonalizado}", NuevoHorario("Nuevo título"));
        Assert.Equal("Nuevo título", (await put3.JsonAsync()).Str("titulo"));
    }

    [Fact]
    public void Titulo_generado_nunca_supera_100_caracteres_y_sin_aula_no_lleva_prefijo()
    {
        var largo = TituloHorario.Generar(new string('x', 300), Dia, Dia.AddDays(1), Dia.AddHours(8), Dia.AddHours(9));
        Assert.True(largo.Length <= TituloHorario.LongitudMaxima);
        Assert.EndsWith(" · 15/03/2030–16/03/2030 08:00–09:00", largo);

        Assert.Equal("15/03/2030 08:00–09:00", TituloHorario.Generar(null, Dia, Dia, Dia.AddHours(8), Dia.AddHours(9)));
    }

    // ------------------------------------------------------------------ 2) Paginación de usuarios

    private void CrearUsuarioBusqueda(string correo, string nombres, string apellidos, int rol) => _f.ConDb(db =>
    {
        if (db.Usuarios.Any(u => u.correo == correo)) return 0;
        db.Usuarios.Add(new AgendamientoGestion.Entidades.Entities.Usuario
        {
            nombres = nombres, apellidos = apellidos, correo = correo, contrasenaHash = "x",
            fechaRegistro = DateTime.Now, Rol_idRol = rol
        });
        return db.SaveChanges();
    });

    [Fact]
    public async Task Get_usuarios_sin_parametros_devuelve_todos_sin_cabecera()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.GetAsync("/api/Usuario");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.False(resp.Headers.Contains("X-Total-Count"));
        var total = _f.ConDb(db => db.Usuarios.Count());
        Assert.Equal(total, (await resp.JsonAsync()).AsArray().Count);
    }

    [Fact]
    public async Task Get_usuarios_paginado_ordena_y_devuelve_total()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var total = _f.ConDb(db => db.Usuarios.Count());

        var resp = await admin.GetAsync("/api/Usuario?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(total.ToString(), resp.Headers.GetValues("X-Total-Count").Single());
        var pagina = (await resp.JsonAsync()).AsArray();
        Assert.Equal(2, pagina.Count);

        var esperado = _f.ConDb(db => db.Usuarios.AsEnumerable()
            .OrderBy(u => u.apellidos, StringComparer.Ordinal).ThenBy(u => u.nombres, StringComparer.Ordinal).ThenBy(u => u.idUsuario)
            .Select(u => u.idUsuario).Take(2).ToList());
        Assert.Equal(esperado, pagina.Select(n => n!["idUsuario"]!.GetValue<int>()).ToList());

        // Página fuera del total: vacía, con total.
        var lejos = await admin.GetAsync("/api/Usuario?page=1000&pageSize=100");
        Assert.Empty((await lejos.JsonAsync()).AsArray());
        Assert.Equal(total.ToString(), lejos.Headers.GetValues("X-Total-Count").Single());

        // pageSize por defecto 20 si solo llega page.
        var porDefecto = await admin.GetAsync("/api/Usuario?page=1");
        Assert.Equal(Math.Min(20, total), (await porDefecto.JsonAsync()).AsArray().Count);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=abc")]
    [InlineData("rol=Inexistente")]
    public async Task Get_usuarios_parametros_fuera_de_rango_400(string query)
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.GetAsync($"/api/Usuario?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await resp.JsonAsync()).Str("message")));
    }

    [Fact]
    public async Task Busqueda_ignora_mayusculas_y_tildes_y_filtra_por_rol_y_activo()
    {
        CrearUsuarioBusqueda("jm.np@test.com", "José María", "Núñez Pérez", Seed.RolEstudiante);
        CrearUsuarioBusqueda("otro.perez@test.com", "Otro", "Perez", Seed.RolDocente);
        using var admin = await _f.ClienteComoAsync(Seed.Admin);

        async Task<List<string>> Correos(string query)
        {
            var resp = await admin.GetAsync($"/api/Usuario?{query}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            return (await resp.JsonAsync()).AsArray().Select(n => n!.Str("correo")!).ToList();
        }

        Assert.Contains("jm.np@test.com", await Correos("q=jose"));
        Assert.Contains("jm.np@test.com", await Correos("q=JOSÉ"));
        Assert.Contains("jm.np@test.com", await Correos("q=nunez"));
        Assert.Contains("jm.np@test.com", await Correos("q=Núñez"));
        Assert.Contains("jm.np@test.com", await Correos("q=" + Uri.EscapeDataString("MARIA nuñez")));
        Assert.Contains("jm.np@test.com", await Correos("q=JM.NP@TEST"));

        var perez = await Correos("q=perez");
        Assert.Contains("jm.np@test.com", perez);
        Assert.Contains("otro.perez@test.com", perez);

        var perezEstudiantes = await Correos("q=pérez&rol=estudiante");
        Assert.Equal(new[] { "jm.np@test.com" }, perezEstudiantes);

        Assert.Empty(await Correos("q=nunez&activo=false"));
        Assert.Empty(await Correos("q=" + Uri.EscapeDataString("%")));
    }

    // ------------------------------------------------------------------ A) Cuenta habilitada

    private async Task<HttpResponseMessage> Patch(HttpClient c, int id, bool? activo) =>
        await c.PatchAsJsonAsync($"/api/Usuario/{id}/estado", new { activo });

    private async Task<HttpResponseMessage> Login(string correo)
    {
        using var c = _f.CreateClient();
        return await c.PostAsJsonAsync("/api/Auth/login", new { correo, contrasena = ApiFactory.Password });
    }

    [Fact]
    public async Task Desactivar_impide_login_invalida_token_y_asignacion_y_reactivar_lo_restablece()
    {
        const string correoTutor = "tutor.inactivo@test.com";
        const string correoEst = "est.inactivo@test.com";
        var tutorId = Datos.CrearUsuario(_f, correoTutor, Seed.RolTutor);
        var estId = Datos.CrearUsuario(_f, correoEst, Seed.RolEstudiante);

        // Token emitido ANTES de desactivar.
        var loginPrevio = await (await Login(correoTutor)).JsonAsync();
        using var tutor = _f.CreateClient();
        tutor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPrevio.Str("token"));
        Assert.Equal(HttpStatusCode.OK, (await tutor.GetAsync("/api/Auth/me")).StatusCode);

        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var r = await Patch(admin, tutorId, false);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.False((await r.JsonAsync())["activo"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.OK, (await Patch(admin, estId, false)).StatusCode);

        // Login → 403 con mensaje.
        var login = await Login(correoTutor);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Contains("desactivada", (await login.JsonAsync()).Str("message"));

        // Token previo → 401 en /me y en cualquier endpoint.
        var me = await tutor.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Contains("desactivada", (await me.JsonAsync()).Str("message"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await tutor.GetAsync("/api/Horario/disponibles")).StatusCode);

        // Asignar tutoría al inactivo → 400; agregar estudiante inactivo → 400.
        var horarioId = Datos.CrearHorario(_f, Seed.Docente);
        var crear = await admin.PostAsJsonAsync("/api/Tutoria", new
        {
            idioma = "Inglés", nivel = "B1", tema = "Inactivo", modalidad = "Virtual", estado = "Pendiente",
            fechaTutoria = DateTime.Today.AddHours(8), usuarioId = tutorId, horarioId, cupos = 5
        });
        Assert.Equal(HttpStatusCode.BadRequest, crear.StatusCode);

        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente");
        var agregar = await admin.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { estId } });
        Assert.Equal(HttpStatusCode.BadRequest, agregar.StatusCode);

        // by-rol excluye inactivos salvo ?incluirInactivos=true.
        var tutores = (await (await admin.GetAsync("/api/Usuario/by-rol/Tutor")).JsonAsync()).AsArray();
        Assert.DoesNotContain(tutores, n => n!["idUsuario"]!.GetValue<int>() == tutorId);
        var todos = (await (await admin.GetAsync("/api/Usuario/by-rol/Tutor?incluirInactivos=true")).JsonAsync()).AsArray();
        Assert.Contains(todos, n => n!["idUsuario"]!.GetValue<int>() == tutorId);

        // PUT /Usuario no reactiva.
        var put = await admin.PutAsJsonAsync($"/api/Usuario/{tutorId}", new
        {
            nombres = "Tutor", apellidos = "Editado", correo = correoTutor, rolId = Seed.RolTutor
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.False((await put.JsonAsync())["activo"]!.GetValue<bool>());

        // Reactivar.
        Assert.Equal(HttpStatusCode.OK, (await Patch(admin, tutorId, true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(correoTutor)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await tutor.GetAsync("/api/Auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Patch(admin, estId, true)).StatusCode);
    }

    [Fact]
    public async Task Auxiliar_solo_cambia_estado_de_estudiantes()
    {
        var estId = Datos.CrearUsuario(_f, "est.aux@test.com", Seed.RolEstudiante);
        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);

        var r = await Patch(aux, Id(Seed.Docente), false);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Patch(aux, Id(Seed.Admin), false)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Patch(aux, estId, false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Patch(aux, estId, true)).StatusCode);

        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        Assert.Equal(HttpStatusCode.Forbidden, (await Patch(docente, estId, false)).StatusCode);
    }

    [Fact]
    public async Task No_se_desactiva_a_si_mismo_ni_al_ultimo_admin_y_valida_cuerpo()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var adminId = Id(Seed.Admin);

        // Único Administrador activo → 409 "último Administrador".
        var ultimo = await Patch(admin, adminId, false);
        Assert.Equal(HttpStatusCode.Conflict, ultimo.StatusCode);
        Assert.Contains("último Administrador", (await ultimo.JsonAsync()).Str("message"));

        // Con un segundo Administrador: desactivarse a sí mismo sigue siendo 409 (otro mensaje).
        const string admin2 = "admin2@test.com";
        var admin2Id = Datos.CrearUsuario(_f, admin2, Seed.RolAdministrador);
        using var cAdmin2 = await _f.ClienteComoAsync(admin2);
        var propio = await Patch(cAdmin2, admin2Id, false);
        Assert.Equal(HttpStatusCode.Conflict, propio.StatusCode);
        Assert.Contains("propia cuenta", (await propio.JsonAsync()).Str("message"));

        // Un administrador sí puede desactivar a otro si queda alguno activo.
        Assert.Equal(HttpStatusCode.OK, (await Patch(admin, admin2Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Patch(admin, admin2Id, true)).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await Patch(admin, adminId, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Patch(admin, 999999, false)).StatusCode);
    }

    // ------------------------------------------------------------------ B) Atendidos

    [Fact]
    public async Task Atendidos_post_idempotente_get_y_delete()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);

        var p1 = await admin.PostAsJsonAsync("/api/Atencion/atendidos", new { tipo = "tutorias-vencidas", claves = new[] { 101, 102, 102 } });
        Assert.Equal(HttpStatusCode.OK, p1.StatusCode);
        var r1 = (await p1.JsonAsync()).AsArray();
        Assert.Equal(2, r1.Count);
        Assert.All(r1, n => Assert.Equal(Id(Seed.Admin), n!["usuarioId"]!.GetValue<int>()));
        Assert.All(r1, n => Assert.EndsWith("Z", n!["fecha"]!.GetValue<string>()));
        var fecha101 = r1[0]!["fecha"]!.GetValue<string>();

        // El Auxiliar repite 101 y añade 103: 101 conserva quién/cuándo, 103 es suyo.
        var p2 = await aux.PostAsJsonAsync("/api/Atencion/atendidos", new { tipo = "Tutorias-Vencidas", claves = new[] { 101, 103 } });
        var r2 = (await p2.JsonAsync()).AsArray();
        Assert.Equal(Id(Seed.Admin), r2.Single(n => n!["clave"]!.GetValue<int>() == 101)!["usuarioId"]!.GetValue<int>());
        Assert.Equal(fecha101, r2.Single(n => n!["clave"]!.GetValue<int>() == 101)!["fecha"]!.GetValue<string>());
        var de103 = r2.Single(n => n!["clave"]!.GetValue<int>() == 103)!;
        Assert.Equal(Id(Seed.Auxiliar), de103["usuarioId"]!.GetValue<int>());
        Assert.Equal("aux Prueba", de103.Str("usuarioNombre")); // nombres + " " + apellidos
        Assert.Equal("tutorias-vencidas", de103.Str("tipo"));

        var get = (await (await aux.GetAsync("/api/Atencion/atendidos?tipo=tutorias-vencidas")).JsonAsync()).AsArray();
        Assert.Equal(new[] { 101, 102, 103 }, get.Select(n => n!["clave"]!.GetValue<int>()).ToArray());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/Atencion/atendidos/tutorias-vencidas/102")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/Atencion/atendidos/tutorias-vencidas/102")).StatusCode);
        var tras = (await (await admin.GetAsync("/api/Atencion/atendidos")).JsonAsync()).AsArray();
        Assert.DoesNotContain(tras, n => n!.Str("tipo") == "tutorias-vencidas" && n!["clave"]!.GetValue<int>() == 102);
    }

    [Theory]
    [InlineData("{\"tipo\":\"otro\",\"claves\":[1]}")]
    [InlineData("{\"tipo\":\"tutorias-vencidas\",\"claves\":[]}")]
    [InlineData("{\"tipo\":\"tutorias-vencidas\",\"claves\":[0]}")]
    [InlineData("{\"tipo\":\"tutorias-vencidas\"}")]
    [InlineData("{\"claves\":[1]}")]
    public async Task Atendidos_post_invalido_400(string cuerpo)
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.PostAsync("/api/Atencion/atendidos", new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await resp.JsonAsync()).Str("message")));
    }

    [Fact]
    public async Task Atendidos_limites_y_autorizacion()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var demasiadas = Enumerable.Range(1, 201).ToArray();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/Atencion/atendidos", new { tipo = "horarios-libres-7d", claves = demasiadas })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/Atencion/atendidos?tipo=nope")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync("/api/Atencion/atendidos/nope/1")).StatusCode);

        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.GetAsync("/api/Atencion/atendidos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await docente.PostAsJsonAsync("/api/Atencion/atendidos", new { tipo = "horarios-libres-7d", claves = new[] { 1 } })).StatusCode);
        using var anonimo = _f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/Atencion/atendidos")).StatusCode);
    }

    [Fact]
    public async Task Encuestas_pendientes_por_tutoria()
    {
        var tutoriaId = Datos.CrearTutoria(_f, "Finalizada", Seed.Estudiante1, Seed.Estudiante2);
        _f.ConDb(db =>
        {
            db.EncuestasSatisfaccion.AddRange(
                new AgendamientoGestion.Entidades.Entities.EncuestaSatisfaccion
                    { Tutoria_idTutoria = tutoriaId, tokenRespuesta = Guid.NewGuid().ToString("N"), estudianteCorreo = Seed.Estudiante1 },
                new AgendamientoGestion.Entidades.Entities.EncuestaSatisfaccion
                    { Tutoria_idTutoria = tutoriaId, tokenRespuesta = Guid.NewGuid().ToString("N"), estudianteCorreo = Seed.Estudiante2, tokenUsado = true, calificacion = 5 });
            return db.SaveChanges();
        });

        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);
        var resp = await aux.GetAsync("/api/Encuesta/pendientes");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var fila = (await resp.JsonAsync()).AsArray().Single(n => n!["tutoriaId"]!.GetValue<int>() == tutoriaId)!;
        Assert.Equal(2, fila["enviadas"]!.GetValue<int>());
        Assert.Equal(1, fila["sinResponder"]!.GetValue<int>());
        Assert.Equal("Prueba", fila.Str("tema"));

        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.GetAsync("/api/Encuesta/pendientes")).StatusCode);
    }

    [Fact]
    public async Task Cors_expone_X_Total_Count()
    {
        using var c = _f.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/Usuario?page=1");
        req.Headers.Add("Origin", "http://localhost:5173");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _f.TokenDeAsync(Seed.Admin));
        var resp = await c.SendAsync(req);
        Assert.Contains("X-Total-Count", string.Join(",", resp.Headers.GetValues("Access-Control-Expose-Headers")));
    }
}
