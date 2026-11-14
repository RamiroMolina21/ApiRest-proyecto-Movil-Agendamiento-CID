using Agendamiento.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Agendamiento.Tests;

public class CambioContrasenaTests : IClassFixture<ApiFactory>
{
    private const string Url = "/api/Auth/contrasena";
    private readonly ApiFactory _f;

    public CambioContrasenaTests(ApiFactory factory) => _f = factory;

    /// <summary>Cliente con token recién emitido para un usuario dedicado al test (no comparte caché de tokens).</summary>
    private async Task<HttpClient> ClienteDe(string correo, string contrasena)
    {
        var c = _f.CreateClient();
        var login = await c.PostAsJsonAsync("/api/Auth/login", new { correo, contrasena });
        login.EnsureSuccessStatusCode();
        var token = (await login.JsonAsync()).Str("token");
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<HttpStatusCode> Login(string correo, string contrasena)
    {
        using var c = _f.CreateClient();
        return (await c.PostAsJsonAsync("/api/Auth/login", new { correo, contrasena })).StatusCode;
    }

    [Theory]
    [InlineData(Seed.RolAdministrador, "cc.admin@test.com")]
    [InlineData(Seed.RolAuxiliar, "cc.aux@test.com")]
    [InlineData(Seed.RolDocente, "cc.docente@test.com")]
    [InlineData(Seed.RolTutor, "cc.tutor@test.com")]
    public async Task Los_cuatro_roles_cambian_su_contrasena_y_la_antigua_deja_de_valer(int rolId, string correo)
    {
        Datos.CrearUsuario(_f, correo, rolId);
        using var c = await ClienteDe(correo, ApiFactory.Password);

        var resp = await c.PutAsJsonAsync(Url, new { contrasenaActual = ApiFactory.Password, contrasenaNueva = "NuevaClave-2026" });

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await Login(correo, ApiFactory.Password));
        Assert.Equal(HttpStatusCode.OK, await Login(correo, "NuevaClave-2026"));

        // El token emitido antes del cambio sigue siendo válido hasta expirar (documentado).
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/Auth/me")).StatusCode);
    }

    [Fact]
    public async Task Solo_cambia_la_del_usuario_del_token()
    {
        const string correo = "cc.solo@test.com";
        Datos.CrearUsuario(_f, correo, Seed.RolDocente);
        var hashAdminAntes = _f.ConDb(db => db.Usuarios.Single(u => u.correo == Seed.Admin).contrasenaHash);
        using var c = await ClienteDe(correo, ApiFactory.Password);

        // Campos extra (idUsuario/correo) se ignoran: no hay forma de apuntar a otro usuario.
        var resp = await c.PutAsJsonAsync(Url, new
        {
            contrasenaActual = ApiFactory.Password, contrasenaNueva = "OtraClave-2026",
            idUsuario = _f.ConDb(db => Seed.IdDe(db, Seed.Admin)), correo = Seed.Admin
        });

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.Equal(hashAdminAntes, _f.ConDb(db => db.Usuarios.Single(u => u.correo == Seed.Admin).contrasenaHash));
        Assert.Equal(HttpStatusCode.OK, await Login(correo, "OtraClave-2026"));
    }

    [Theory]
    [InlineData("IncorrectaX1", "ValidaNueva-1", "La contraseña actual no es correcta")]
    [InlineData(ApiFactory.Password, "corta", "La nueva contraseña debe tener entre 8 y 72 caracteres")]
    [InlineData(ApiFactory.Password, ApiFactory.Password + "x", null)] // válida (control)
    public async Task Validaciones_devuelven_400_con_mensaje(string actual, string nueva, string? mensajeEsperado)
    {
        var correo = $"cc.val.{Guid.NewGuid():N}@test.com";
        Datos.CrearUsuario(_f, correo, Seed.RolTutor);
        using var c = await ClienteDe(correo, ApiFactory.Password);

        var resp = await c.PutAsJsonAsync(Url, new { contrasenaActual = actual, contrasenaNueva = nueva });

        if (mensajeEsperado == null)
        {
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
            return;
        }

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        // Cuerpo ProblemDetails con "message" (los 400 de validación de modelo en AuthController salen como
        // application/json por su [Produces]; los de negocio como application/problem+json).
        var json = await resp.JsonAsync();
        Assert.Contains(mensajeEsperado, json.Str("message"));
        Assert.Equal(HttpStatusCode.OK, await Login(correo, ApiFactory.Password)); // no cambió
    }

    [Fact]
    public async Task Nueva_igual_a_la_actual_o_al_correo_da_400()
    {
        const string correo = "cc.igual@test.com";
        Datos.CrearUsuario(_f, correo, Seed.RolDocente);
        using var c = await ClienteDe(correo, ApiFactory.Password);

        var igual = await c.PutAsJsonAsync(Url, new { contrasenaActual = ApiFactory.Password, contrasenaNueva = ApiFactory.Password });
        Assert.Equal(HttpStatusCode.BadRequest, igual.StatusCode);
        Assert.Equal("La nueva contraseña debe ser distinta de la actual", (await igual.JsonAsync()).Str("message"));

        var correoComoClave = await c.PutAsJsonAsync(Url, new { contrasenaActual = ApiFactory.Password, contrasenaNueva = correo.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.BadRequest, correoComoClave.StatusCode);
        Assert.Equal("La nueva contraseña no puede ser igual a su correo", (await correoComoClave.JsonAsync()).Str("message"));
    }

    [Fact]
    public async Task Sin_token_401_y_usuario_borrado_401()
    {
        using var anon = _f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anon.PutAsJsonAsync(Url, new { contrasenaActual = "x", contrasenaNueva = "ValidaNueva-1" })).StatusCode);

        const string correo = "cc.borrado@test.com";
        var id = Datos.CrearUsuario(_f, correo, Seed.RolDocente);
        using var c = await ClienteDe(correo, ApiFactory.Password);
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/Usuario/{id}")).StatusCode);

        var resp = await c.PutAsJsonAsync(Url, new { contrasenaActual = ApiFactory.Password, contrasenaNueva = "ValidaNueva-1" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}

public class LimiteCambioContrasenaFactory : ApiFactory
{
    protected override IDictionary<string, string?> Configuracion
    {
        get
        {
            var cfg = base.Configuracion;
            cfg["RateLimiting:CambioContrasena:PermitLimit"] = "2";
            return cfg;
        }
    }
}

public class CambioContrasenaRateLimitTests : IClassFixture<LimiteCambioContrasenaFactory>
{
    private readonly LimiteCambioContrasenaFactory _f;
    public CambioContrasenaRateLimitTests(LimiteCambioContrasenaFactory f) => _f = f;

    [Fact]
    public async Task Supera_el_limite_por_usuario_y_devuelve_429_sin_afectar_a_otro_usuario()
    {
        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        var malo = new { contrasenaActual = "IncorrectaX1", contrasenaNueva = "ValidaNueva-1" };

        Assert.Equal(HttpStatusCode.BadRequest, (await docente.PutAsJsonAsync("/api/Auth/contrasena", malo)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await docente.PutAsJsonAsync("/api/Auth/contrasena", malo)).StatusCode);
        var tercera = await docente.PutAsJsonAsync("/api/Auth/contrasena", malo);
        Assert.Equal(HttpStatusCode.TooManyRequests, tercera.StatusCode);
        Assert.True(tercera.Headers.Contains("Retry-After"));

        // Misma IP, otro usuario: su propio cupo (partición por "sub").
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);
        Assert.Equal(HttpStatusCode.BadRequest, (await tutor.PutAsJsonAsync("/api/Auth/contrasena", malo)).StatusCode);
    }
}
