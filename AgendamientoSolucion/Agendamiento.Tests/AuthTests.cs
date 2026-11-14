using Agendamiento.Tests.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Agendamiento.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public AuthTests(ApiFactory factory) => _f = factory;

    private Task<HttpResponseMessage> Login(HttpClient c, string correo, string pwd) =>
        c.PostAsJsonAsync("/api/Auth/login", new { correo, contrasena = pwd });

    [Fact]
    public async Task Login_correcto_devuelve_token_expiracion_y_usuario()
    {
        using var c = _f.CreateClient();
        var resp = await Login(c, Seed.Docente, ApiFactory.Password);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.JsonAsync();
        Assert.Equal("Login exitoso", json.Str("message"));
        Assert.Equal("Bearer", json.Str("tokenType"));
        Assert.False(string.IsNullOrWhiteSpace(json.Str("token")));
        Assert.EndsWith("Z", json.Str("expiresAt"));
        Assert.Equal(Seed.Docente, json["usuario"]!.Str("correo"));
        Assert.Equal("Docente", json["usuario"]!.Str("tipoRol"));
        Assert.Equal(Seed.RolDocente, json["usuario"]!["rolId"]!.GetValue<int>());
    }

    [Fact]
    public async Task Token_contiene_claims_sub_correo_rol_y_expira_en_8_horas()
    {
        var token = await _f.TokenDeAsync(Seed.Auxiliar);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        var idEsperado = _f.ConDb(db => Seed.IdDe(db, Seed.Auxiliar));

        Assert.Equal(idEsperado.ToString(), jwt.Subject);
        Assert.Equal(Seed.Auxiliar, jwt.GetClaim("correo").Value);
        Assert.Equal("Auxiliar", jwt.GetClaim("rol").Value);
        Assert.Equal("AgendamientoApi", jwt.Issuer);
        Assert.InRange(jwt.ValidTo - jwt.IssuedAt, TimeSpan.FromHours(8) - TimeSpan.FromSeconds(5), TimeSpan.FromHours(8) + TimeSpan.FromSeconds(5));
        Assert.Equal("HS256", jwt.Alg);
    }

    [Fact]
    public async Task Correo_inexistente_y_contrasena_erronea_dan_el_mismo_401_generico()
    {
        using var c = _f.CreateClient();
        var noExiste = await Login(c, "nadie@test.com", ApiFactory.Password);
        var malaPwd = await Login(c, Seed.Docente, "Incorrecta1");

        Assert.Equal(HttpStatusCode.Unauthorized, noExiste.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, malaPwd.StatusCode);
        var m1 = (await noExiste.JsonAsync()).Str("message");
        var m2 = (await malaPwd.JsonAsync()).Str("message");
        Assert.Equal("Correo o contraseña incorrectos", m1);
        Assert.Equal(m1, m2);
        Assert.Equal("application/problem+json", noExiste.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Hash_legacy_no_bcrypt_da_401_y_no_500()
    {
        using var c = _f.CreateClient();
        var resp = await Login(c, Seed.Legacy, "hash12345");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Estudiante_con_credenciales_correctas_recibe_403()
    {
        using var c = _f.CreateClient();
        var resp = await Login(c, Seed.Estudiante1, ApiFactory.Password);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("El usuario no tiene acceso a la aplicación", (await resp.JsonAsync()).Str("message"));
    }

    [Fact]
    public async Task Correo_con_formato_invalido_da_400_problem_details_con_errors_y_message()
    {
        using var c = _f.CreateClient();
        var resp = await Login(c, "no-es-correo", ApiFactory.Password);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var json = await resp.JsonAsync();
        Assert.NotNull(json["errors"]);
        Assert.Contains("correo", json.Str("message"), StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(json.Str("traceId"));
    }

    [Fact]
    public async Task Me_con_token_devuelve_usuario_y_sin_token_401()
    {
        using var anon = _f.CreateClient();
        var sinToken = await anon.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.False(string.IsNullOrEmpty((await sinToken.JsonAsync()).Str("message")));

        using var c = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await c.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(Seed.Admin, (await resp.JsonAsync()).Str("correo"));
    }

    [Fact]
    public async Task Token_firmado_con_otra_clave_o_expirado_da_401()
    {
        var id = _f.ConDb(db => Seed.IdDe(db, Seed.Admin)).ToString();

        string Crear(string key, DateTime expira) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "AgendamientoApi",
            Audience = "AgendamientoClientes",
            IssuedAt = expira.AddHours(-1),
            NotBefore = expira.AddHours(-1),
            Expires = expira,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object> { ["sub"] = id, ["rol"] = "Administrador", ["correo"] = Seed.Admin }
        });

        var otraClave = Crear("otra-clave-distinta-de-la-real-0123456789abcdef", DateTime.UtcNow.AddHours(1));
        var expirado = Crear(ApiFactory.JwtKeyDePrueba, DateTime.UtcNow.AddMinutes(-10));
        var valido = Crear(ApiFactory.JwtKeyDePrueba, DateTime.UtcNow.AddHours(1));

        foreach (var (token, esperado) in new[] { (otraClave, HttpStatusCode.Unauthorized), (expirado, HttpStatusCode.Unauthorized), (valido, HttpStatusCode.OK) })
        {
            using var c = _f.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await c.GetAsync("/api/Metricas/generales");
            Assert.Equal(esperado, resp.StatusCode);
        }
    }
}

public class LimiteLoginFactory : ApiFactory
{
    protected override IDictionary<string, string?> Configuracion
    {
        get
        {
            var cfg = base.Configuracion;
            cfg["RateLimiting:Login:PermitLimit"] = "2";
            return cfg;
        }
    }
}

public class RateLimitTests : IClassFixture<LimiteLoginFactory>
{
    private readonly LimiteLoginFactory _f;
    public RateLimitTests(LimiteLoginFactory f) => _f = f;

    [Fact]
    public async Task Login_supera_el_limite_por_ip_y_devuelve_429_con_retry_after()
    {
        using var c = _f.CreateClient();
        var body = new { correo = "nadie@test.com", contrasena = "Incorrecta1" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/Auth/login", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/Auth/login", body)).StatusCode);

        var tercera = await c.PostAsJsonAsync("/api/Auth/login", body);
        Assert.Equal(HttpStatusCode.TooManyRequests, tercera.StatusCode);
        Assert.True(tercera.Headers.Contains("Retry-After"));
        Assert.False(string.IsNullOrEmpty((await tercera.JsonAsync()).Str("message")));
    }
}

public class SinClaveJwtFactory : ApiFactory
{
    protected override IDictionary<string, string?> Configuracion
    {
        get
        {
            var cfg = base.Configuracion;
            cfg["Jwt:Key"] = "corta";
            return cfg;
        }
    }
}

public class ArranqueTests
{
    [Fact]
    public void La_api_no_arranca_si_Jwt_Key_es_corta_y_el_error_lo_explica()
    {
        using var f = new SinClaveJwtFactory();
        var ex = Assert.ThrowsAny<Exception>(() => f.CreateClient());
        var mensaje = ex.ToString();
        Assert.Contains("Jwt:Key", mensaje);
    }
}
