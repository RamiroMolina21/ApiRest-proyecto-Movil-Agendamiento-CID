using Agendamiento.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Agendamiento.Tests;

public class AutorizacionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public AutorizacionTests(ApiFactory factory) => _f = factory;

    [Theory]
    [InlineData("GET", "/api/Usuario")]
    [InlineData("GET", "/api/Usuario/by-rol/Estudiante")]
    [InlineData("GET", "/api/Horario")]
    [InlineData("GET", "/api/Horario/disponibles")]
    [InlineData("GET", "/api/Tutoria")]
    [InlineData("PUT", "/api/Tutoria/1/finalizar")]
    [InlineData("GET", "/api/Metricas/generales")]
    [InlineData("GET", "/api/Notificacion")]
    [InlineData("GET", "/api/Retroalimentacion")]
    [InlineData("POST", "/api/Encuesta/enviar/1")]
    [InlineData("GET", "/api/Encuesta/estadisticas")]
    [InlineData("GET", "/api/Ruta/Que/No/Existe")]
    public async Task Sin_token_devuelve_401_problem_details(string metodo, string url)
    {
        using var c = _f.CreateClient();
        var resp = await c.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        Assert.False(string.IsNullOrEmpty((await resp.JsonAsync()).Str("message")));
    }

    [Theory]
    [InlineData("GET", "/api/Usuario")]
    [InlineData("GET", "/api/Metricas/generales")]
    [InlineData("GET", "/api/Notificacion")]
    [InlineData("GET", "/api/Tutoria")]
    [InlineData("GET", "/api/Horario")]
    [InlineData("DELETE", "/api/Tutoria/1")]
    [InlineData("GET", "/api/Encuesta/estadisticas")]
    public async Task Docente_recibe_403_en_endpoints_de_gestion_web(string metodo, string url)
    {
        using var c = await _f.ClienteComoAsync(Seed.Docente);
        var resp = await c.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.False(string.IsNullOrEmpty((await resp.JsonAsync()).Str("message")));
    }

    [Theory]
    [InlineData(Seed.Docente)]
    [InlineData(Seed.Tutor)]
    public async Task Docente_y_Tutor_pueden_usar_los_endpoints_de_MAUI(string correo)
    {
        using var c = await _f.ClienteComoAsync(correo);
        var miId = _f.ConDb(db => Seed.IdDe(db, correo));

        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/Horario/disponibles")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/Horario/usuario/{miId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/Usuario/by-rol/Estudiante")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/Tutoria/estado/Pendiente/usuario/{miId}")).StatusCode);

        var tutoriaId = Datos.CrearTutoria(_f, "Pendiente", Seed.Estudiante1);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/Tutoria/{tutoriaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/Tutoria/{tutoriaId}/estudiantes")).StatusCode);
    }

    [Fact]
    public async Task Docente_no_puede_consultar_horarios_ni_tutorias_de_otro_usuario()
    {
        using var c = await _f.ClienteComoAsync(Seed.Docente);
        var otroId = _f.ConDb(db => Seed.IdDe(db, Seed.Tutor));

        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/Horario/usuario/{otroId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/Tutoria/estado/Pendiente/usuario/{otroId}")).StatusCode);

        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/Horario/usuario/{otroId}")).StatusCode);
    }

    [Fact]
    public async Task Auxiliar_crea_estudiantes_pero_no_puede_crear_ni_borrar_personal()
    {
        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);

        var est = await aux.PostAsJsonAsync("/api/Usuario", NuevoUsuario("nuevo.est@test.com", Seed.RolEstudiante));
        Assert.Equal(HttpStatusCode.Created, est.StatusCode);

        var admin = await aux.PostAsJsonAsync("/api/Usuario", NuevoUsuario("intruso@test.com", Seed.RolAdministrador));
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);

        var docenteId = _f.ConDb(db => Seed.IdDe(db, Seed.Docente));
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.DeleteAsync($"/api/Usuario/{docenteId}")).StatusCode);

        // Auxiliar no puede "promover" a un estudiante.
        var estId = (await est.JsonAsync())["idUsuario"]!.GetValue<int>();
        var promover = await aux.PutAsJsonAsync($"/api/Usuario/{estId}", NuevoUsuario("nuevo.est@test.com", Seed.RolAdministrador));
        Assert.Equal(HttpStatusCode.Forbidden, promover.StatusCode);
    }

    [Fact]
    public async Task Administrador_crea_personal_y_correo_duplicado_da_409()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);

        var creado = await admin.PostAsJsonAsync("/api/Usuario", NuevoUsuario("nuevo.docente@test.com", Seed.RolDocente));
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);

        var duplicado = await admin.PostAsJsonAsync("/api/Usuario", NuevoUsuario("nuevo.docente@test.com", Seed.RolDocente));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal("Ya existe un usuario con este correo", (await duplicado.JsonAsync()).Str("message"));
    }

    [Fact]
    public async Task Cors_permite_el_origen_configurado_y_rechaza_otros()
    {
        using var c = _f.CreateClient();

        HttpRequestMessage Preflight(string origen)
        {
            var req = new HttpRequestMessage(HttpMethod.Options, "/api/Usuario");
            req.Headers.Add("Origin", origen);
            req.Headers.Add("Access-Control-Request-Method", "GET");
            req.Headers.Add("Access-Control-Request-Headers", "authorization");
            return req;
        }

        var ok = await c.SendAsync(Preflight("http://localhost:5173"));
        Assert.True(ok.IsSuccessStatusCode);
        Assert.Equal("http://localhost:5173", ok.Headers.GetValues("Access-Control-Allow-Origin").Single());

        var malo = await c.SendAsync(Preflight("https://sitio-malicioso.example"));
        Assert.False(malo.Headers.Contains("Access-Control-Allow-Origin"));
    }

    internal static object NuevoUsuario(string correo, int rolId) => new
    {
        nombres = "Nuevo",
        apellidos = "Usuario",
        correo,
        contrasena = "Clave1234",
        rolId
    };
}
