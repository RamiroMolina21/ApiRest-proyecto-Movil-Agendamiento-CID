using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Logica.Correo;
using AgendamientoGestion.Logica.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Agendamiento.Tests;

/// <summary>El aula (Horario.espacio) es opcional: se guarda "" y los correos muestran "Por confirmar".</summary>
public class AulaOpcionalTests : IClassFixture<ApiFactory>
{
    private static readonly DateTime Dia = new(2030, 5, 10);
    private readonly ApiFactory _f;

    public AulaOpcionalTests(ApiFactory f) => _f = f;

    private object Horario(string? titulo, object? espacio, bool incluirEspacio = true)
    {
        var d = new Dictionary<string, object?>
        {
            ["titulo"] = titulo, ["fechaInicio"] = Dia, ["fechaFin"] = Dia, ["horaInicio"] = Dia.AddHours(14),
            ["horaFin"] = Dia.AddHours(15), ["cupos"] = "4", ["estado"] = "Disponible",
            ["usuarioId"] = _f.ConDb(db => Seed.IdDe(db, Seed.Docente))
        };
        if (incluirEspacio) d["espacio"] = espacio;
        return d;
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("    ", true)]
    [InlineData(null, false)] // propiedad ausente
    public async Task Crear_sin_aula_guarda_vacio_y_titulo_sin_prefijo(string? espacio, bool incluir)
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var r = await admin.PostAsJsonAsync("/api/Horario", Horario(null, espacio, incluir));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var j = await r.JsonAsync();
        Assert.Equal("", j.Str("espacio"));
        Assert.Equal("10/05/2030 14:00–15:00", j.Str("titulo"));
        var id = j["idHorario"]!.GetValue<int>();
        Assert.Equal("", _f.ConDb(db => db.Horarios.Single(h => h.idHorario == id).espacio));
    }

    [Fact]
    public async Task Aula_de_mas_de_60_caracteres_400()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/Horario", Horario(null, new string('x', 61)))).StatusCode);
    }

    [Fact]
    public async Task Put_regenera_el_titulo_generado_al_poner_y_quitar_aula_y_conserva_el_personalizado()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var id = (await (await admin.PostAsJsonAsync("/api/Horario", Horario(null, ""))).JsonAsync())["idHorario"]!.GetValue<int>();

        var conAula = await (await admin.PutAsJsonAsync($"/api/Horario/{id}", Horario("", "Sala 4"))).JsonAsync();
        Assert.Equal("Sala 4 · 10/05/2030 14:00–15:00", conAula.Str("titulo"));
        Assert.Equal("Sala 4", conAula.Str("espacio"));

        var sinAula = await (await admin.PutAsJsonAsync($"/api/Horario/{id}", Horario(null, "  "))).JsonAsync();
        Assert.Equal("10/05/2030 14:00–15:00", sinAula.Str("titulo"));
        Assert.Equal("", sinAula.Str("espacio"));

        var propio = (await (await admin.PostAsJsonAsync("/api/Horario", Horario("Club de lectura", ""))).JsonAsync())["idHorario"]!.GetValue<int>();
        var put = await (await admin.PutAsJsonAsync($"/api/Horario/{propio}", Horario(null, "Sala 7"))).JsonAsync();
        Assert.Equal("Club de lectura", put.Str("titulo"));
    }

    [Fact]
    public async Task Tutoria_en_horario_sin_aula_responde_espacio_vacio_y_el_correo_dice_Por_confirmar()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var hoy = DateTime.Today;
        var horario = new Dictionary<string, object?>
        {
            ["fechaInicio"] = hoy, ["fechaFin"] = hoy, ["horaInicio"] = hoy.AddHours(8), ["horaFin"] = hoy.AddHours(9),
            ["cupos"] = "4", ["estado"] = "Disponible", ["usuarioId"] = _f.ConDb(db => Seed.IdDe(db, Seed.Docente)), ["espacio"] = ""
        };
        var horarioId = (await (await admin.PostAsJsonAsync("/api/Horario", horario)).JsonAsync())["idHorario"]!.GetValue<int>();
        var est = Datos.CrearUsuario(_f, "aula.est@test.com", Seed.RolEstudiante);
        var r = await admin.PostAsJsonAsync("/api/Tutoria", new
        {
            idioma = "Inglés", nivel = "B1", tema = "Sin aula", modalidad = "Presencial", estado = "Pendiente",
            fechaTutoria = hoy.AddHours(8), usuarioId = _f.ConDb(db => Seed.IdDe(db, Seed.Tutor)), horarioId, estudianteIds = new[] { est }, cupos = 5
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal("", (await r.JsonAsync()).Str("horarioEspacio"));

        var correo = _f.Correo.Enviados.Last(c => c.Destinatario == "aula.est@test.com");
        Assert.Contains("Por confirmar", correo.Html);
        Assert.Contains("Lugar: Por confirmar", correo.Texto);
    }

    [Fact]
    public void Plantillas_con_aula_vacia_muestran_Por_confirmar()
    {
        var p = new PlantillasCorreo(Options.Create(new SmtpSettings()));
        var f = new DateTime(2030, 5, 10, 14, 0, 0);
        foreach (var m in new IModeloCorreo[]
        {
            new NuevaTutoriaCorreo("Ana", 1, "Inglés", "B1", "T", "Presencial", f, ""),
            new RecordatorioTutoriaCorreo("Ana", f, "Inglés", "B1", "T", "Presencial", null),
            new CambioTutoriaCorreo("Ana", 1, f, "Inglés", "B1", "T", "Presencial", "  ", "Laura", null, Array.Empty<string>())
        })
        {
            var c = p.Renderizar(m);
            Assert.Contains("Por confirmar", c.Html);
            Assert.Contains("Lugar: Por confirmar", c.Texto);
        }
    }
}
