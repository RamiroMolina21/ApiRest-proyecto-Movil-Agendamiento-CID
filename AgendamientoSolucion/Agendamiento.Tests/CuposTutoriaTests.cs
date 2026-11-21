using Agendamiento.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Agendamiento.Tests;

/// <summary>
/// La capacidad pasa del horario a la tutoría (2026-11-21): Tutoria.cupos obligatorio en POST, opcional en PUT;
/// tutorías antiguas (cupos null) usan los cupos del horario; Horario.cupos es opcional.
/// </summary>
public class CuposTutoriaTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public CuposTutoriaTests(ApiFactory f) => _f = f;

    private int Id(string correo) => _f.ConDb(db => Seed.IdDe(db, correo));

    private Dictionary<string, object?> NuevaTutoria(int horarioId, object? cupos, bool incluirCupos = true, int[]? estudianteIds = null)
    {
        var d = new Dictionary<string, object?>
        {
            ["idioma"] = "Inglés", ["nivel"] = "B1", ["tema"] = "Cupos", ["modalidad"] = "Virtual", ["estado"] = "Pendiente",
            ["fechaTutoria"] = DateTime.Today.AddHours(8), ["usuarioId"] = Id(Seed.Tutor), ["horarioId"] = horarioId,
            ["estudianteIds"] = estudianteIds
        };
        if (incluirCupos) d["cupos"] = cupos;
        return d;
    }

    [Fact]
    public async Task Post_tutoria_con_cupos_los_guarda_y_los_devuelve()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var horario = Datos.CrearHorario(_f, Seed.Docente, cupos: "");

        var resp = await admin.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(horario, 3));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await resp.JsonAsync();
        Assert.Equal(3, json["cupos"]!.GetValue<int>());
        Assert.Null(json["horarioCupos"]);
        var id = json["idTutoria"]!.GetValue<int>();
        Assert.Equal(3, _f.ConDb(db => db.Tutorias.Single(t => t.idTutoria == id).cupos));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(null, false)] // propiedad ausente
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [InlineData(1001, true)]
    public async Task Post_tutoria_sin_cupos_o_fuera_de_rango_es_400_y_no_reserva(int? cupos, bool incluir)
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var horario = Datos.CrearHorario(_f, Seed.Docente);

        var resp = await admin.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(horario, cupos, incluir));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var msg = (await resp.JsonAsync()).Str("message");
        Assert.Equal(cupos == null ? "Los cupos son obligatorios" : "Los cupos deben ser un número entero entre 1 y 1000", msg);
        Assert.Equal("Disponible", _f.ConDb(db => db.Horarios.Single(h => h.idHorario == horario).estado));
    }

    [Fact]
    public async Task Post_tutoria_con_cupos_no_enteros_es_400()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var horario = Datos.CrearHorario(_f, Seed.Docente);
        var resp = await admin.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(horario, "abc"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task El_limite_es_el_de_la_tutoria_aunque_el_horario_diga_otra_cosa()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var e1 = Id(Seed.Estudiante1);
        var e2 = Id(Seed.Estudiante2);
        var horario = Datos.CrearHorario(_f, Seed.Docente, cupos: "10");

        // Crear con más estudiantes que los cupos de la tutoría → 409 (aunque el horario admitiría 10).
        var excede = await admin.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(horario, 1, estudianteIds: new[] { e1, e2 }));
        Assert.Equal(HttpStatusCode.Conflict, excede.StatusCode);
        Assert.StartsWith("Cupo completo: 0 de 1", (await excede.JsonAsync()).Str("message"));

        var ok = await admin.PostAsJsonAsync("/api/Tutoria", NuevaTutoria(horario, 1, estudianteIds: new[] { e1 }));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var json = await ok.JsonAsync();
        Assert.Equal(1, json["cupos"]!.GetValue<int>());
        Assert.Equal(10, json["horarioCupos"]!.GetValue<int>());
        var id = json["idTutoria"]!.GetValue<int>();

        var lleno = await admin.PostAsJsonAsync($"/api/Tutoria/{id}/estudiantes", new { estudianteIds = new[] { e2 } });
        Assert.Equal(HttpStatusCode.Conflict, lleno.StatusCode);
        Assert.StartsWith("Cupo completo: 1 de 1", (await lleno.JsonAsync()).Str("message"));
    }

    [Fact]
    public async Task Tutoria_antigua_sin_cupos_usa_los_del_horario_o_sin_limite()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var e1 = Id(Seed.Estudiante1);
        var e2 = Id(Seed.Estudiante2);

        var antigua = Datos.CrearTutoriaEn(_f, Datos.CrearHorario(_f, Seed.Docente, "Ocupado", cupos: "1"), Seed.Tutor);
        var get = await (await admin.GetAsync($"/api/Tutoria/{antigua}")).JsonAsync();
        Assert.Equal(1, get["cupos"]!.GetValue<int>());
        var r = await admin.PostAsJsonAsync($"/api/Tutoria/{antigua}/estudiantes", new { estudianteIds = new[] { e1, e2 } });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.StartsWith("Cupo completo: 0 de 1", (await r.JsonAsync()).Str("message"));

        var sinLimite = Datos.CrearTutoriaEn(_f, Datos.CrearHorario(_f, Seed.Docente, "Ocupado", cupos: ""), Seed.Tutor);
        Assert.Null((await (await admin.GetAsync($"/api/Tutoria/{sinLimite}")).JsonAsync())["cupos"]);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PostAsJsonAsync($"/api/Tutoria/{sinLimite}/estudiantes", new { estudianteIds = new[] { e1, e2 } })).StatusCode);
    }

    private object CuerpoPut(int horarioId, int? cupos) => new
    {
        idioma = "Inglés", nivel = "A2", tema = "Editada", modalidad = "Virtual", estado = "Pendiente",
        fechaTutoria = DateTime.Today.AddHours(8), usuarioId = Id(Seed.Tutor), horarioId, cupos
    };

    [Fact]
    public async Task Put_cupos_opcional_conserva_y_no_baja_de_los_inscritos()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var horario = Datos.CrearHorario(_f, Seed.Docente, cupos: "");
        var crear = await admin.PostAsJsonAsync("/api/Tutoria",
            NuevaTutoria(horario, 3, estudianteIds: new[] { Id(Seed.Estudiante1), Id(Seed.Estudiante2) }));
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var id = (await crear.JsonAsync())["idTutoria"]!.GetValue<int>();

        // Sin cupos → conserva.
        var conserva = await admin.PutAsJsonAsync($"/api/Tutoria/{id}", CuerpoPut(horario, null));
        Assert.Equal(HttpStatusCode.OK, conserva.StatusCode);
        Assert.Equal(3, (await conserva.JsonAsync())["cupos"]!.GetValue<int>());

        // Por debajo de los 2 inscritos → 409 y no cambia nada.
        var baja = await admin.PutAsJsonAsync($"/api/Tutoria/{id}", CuerpoPut(horario, 1));
        Assert.Equal(HttpStatusCode.Conflict, baja.StatusCode);
        Assert.Equal("No se pueden fijar 1 cupos: la tutoría ya tiene 2 estudiantes inscritos", (await baja.JsonAsync()).Str("message"));
        Assert.Equal(3, _f.ConDb(db => db.Tutorias.Single(t => t.idTutoria == id).cupos));

        // Fuera de rango → 400.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/Tutoria/{id}", CuerpoPut(horario, 0))).StatusCode);

        // Igual a los inscritos → OK.
        var igual = await admin.PutAsJsonAsync($"/api/Tutoria/{id}", CuerpoPut(horario, 2));
        Assert.Equal(HttpStatusCode.OK, igual.StatusCode);
        Assert.Equal(2, _f.ConDb(db => db.Tutorias.Single(t => t.idTutoria == id).cupos));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public async Task Horario_sin_cupos_se_guarda_vacio(string? cupos, bool incluir)
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var d = new Dictionary<string, object?>
        {
            ["titulo"] = "Sin cupos", ["fechaInicio"] = DateTime.Today, ["fechaFin"] = DateTime.Today,
            ["horaInicio"] = DateTime.Today.AddHours(10), ["horaFin"] = DateTime.Today.AddHours(11),
            ["espacio"] = "Sala", ["estado"] = "Disponible", ["usuarioId"] = Id(Seed.Docente)
        };
        if (incluir) d["cupos"] = cupos;

        var resp = await admin.PostAsJsonAsync("/api/Horario", d);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await resp.JsonAsync();
        Assert.Equal("", json.Str("cupos"));
        var id = json["idHorario"]!.GetValue<int>();

        // PUT sin cupos también guarda "" (y con valor se sigue validando).
        d["cupos"] = "7";
        Assert.Equal("7", (await (await admin.PutAsJsonAsync($"/api/Horario/{id}", d)).JsonAsync()).Str("cupos"));
        d["cupos"] = "0";
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/Horario/{id}", d)).StatusCode);
        d.Remove("cupos");
        Assert.Equal("", (await (await admin.PutAsJsonAsync($"/api/Horario/{id}", d)).JsonAsync()).Str("cupos"));
    }
}
