using Agendamiento.Services;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Persistencia.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Agendamiento.Tests.Infrastructure;

/// <summary>
/// Levanta la API completa en memoria con una BD SQLite en memoria (aislada, nunca la BD real),
/// SMTP falso y sin el servicio de recordatorios en segundo plano.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKeyDePrueba = "clave-de-pruebas-solo-para-tests-0123456789abcdef";
    public const string UrlEncuestaDePrueba = "https://web.pruebas.local/encuesta";
    public const string UrlWebDePrueba = "https://web.pruebas.local";
    public const string Password = "Secreta123";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Dictionary<string, string> _tokens = new();
    private readonly SemaphoreSlim _tokensLock = new(1, 1);

    public FakeCorreoService Correo { get; } = new();

    /// <summary>Cuenta los comandos SQL ejecutados (para detectar N+1 en los tests de rendimiento).</summary>
    public ContadorConsultas Consultas { get; } = new();

    /// <summary>Permite a subclases cambiar configuración (p. ej. límite de login o clave JWT).</summary>
    protected virtual IDictionary<string, string?> Configuracion => new Dictionary<string, string?>
    {
        ["Jwt:Key"] = JwtKeyDePrueba,
        ["Jwt:ExpirationMinutes"] = "480",
        ["RateLimiting:Login:PermitLimit"] = "1000",
        ["RateLimiting:Login:WindowSeconds"] = "60",
        ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
        ["Encuesta:UrlBase"] = UrlEncuestaDePrueba,
        ["Web:UrlBase"] = UrlWebDePrueba,
        ["RestablecimientoContrasena:SolicitudesPorIp"] = "1000",
        ["RestablecimientoContrasena:IntentosRestablecerPorIp"] = "1000",
        ["RestablecimientoContrasena:SolicitudesPorCorreo"] = "1000",
        ["ConnectionStrings:local"] = "no-se-usa-en-pruebas"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Configuracion));

        builder.ConfigureTestServices(services =>
        {
            // Reemplazar SQL Server por SQLite en memoria (EF Core 9 compone configuraciones: quitar ambas).
            services.RemoveAll<DbContextOptions<AgendamientoDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AgendamientoDbContext>>();
            SqliteCompatSqlServer.Registrar(_connection);
            _connection.Open();
            services.AddDbContext<AgendamientoDbContext>(o => o.UseSqlite(_connection).AddInterceptors(Consultas));

            services.RemoveAll<ICorreoService>();
            services.AddSingleton<ICorreoService>(Correo);

            var recordatorios = services.Where(d => d.ImplementationType == typeof(RecordatorioTutoriaBackgroundService)).ToList();
            foreach (var d in recordatorios) services.Remove(d);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgendamientoDbContext>();
        db.Database.EnsureCreated();
        Seed.Aplicar(db);
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    public T ConDb<T>(Func<AgendamientoDbContext, T> accion)
    {
        using var scope = Services.CreateScope();
        return accion(scope.ServiceProvider.GetRequiredService<AgendamientoDbContext>());
    }

    public async Task<string> TokenDeAsync(string correo)
    {
        await _tokensLock.WaitAsync();
        try
        {
            if (_tokens.TryGetValue(correo, out var cached)) return cached;
            using var client = CreateClient();
            var resp = await client.PostAsJsonAsync("/api/Auth/login", new { correo, contrasena = Password });
            resp.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var token = json.RootElement.GetProperty("token").GetString()!;
            _tokens[correo] = token;
            return token;
        }
        finally
        {
            _tokensLock.Release();
        }
    }

    public async Task<HttpClient> ClienteComoAsync(string correo)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenDeAsync(correo));
        return client;
    }
}

/// <summary>Datos semilla de pruebas. Contraseñas BCrypt reales (factor bajo para velocidad).</summary>
public static class Seed
{
    public const string Admin = "admin@test.com";
    public const string Auxiliar = "aux@test.com";
    public const string Docente = "docente@test.com";
    public const string Tutor = "tutor@test.com";
    public const string Estudiante1 = "est1@test.com";
    public const string Estudiante2 = "est2@test.com";
    public const string Legacy = "legacy@test.com";

    public const int RolAdministrador = 1, RolTutor = 2, RolDocente = 3, RolEstudiante = 4, RolAuxiliar = 5;

    public static void Aplicar(AgendamientoDbContext db)
    {
        if (db.Roles.Any()) return;

        db.Roles.AddRange(
            new Rol { idRol = RolAdministrador, tipoRol = "Administrador" },
            new Rol { idRol = RolTutor, tipoRol = "Tutor" },
            new Rol { idRol = RolDocente, tipoRol = "Docente" },
            new Rol { idRol = RolEstudiante, tipoRol = "Estudiante" },
            new Rol { idRol = RolAuxiliar, tipoRol = "Auxiliar" });

        var hash = BCrypt.Net.BCrypt.HashPassword(ApiFactory.Password, 4);
        Usuario U(string correo, int rol, string? h = null) => new()
        {
            nombres = correo.Split('@')[0], apellidos = "Prueba", correo = correo,
            contrasenaHash = h ?? hash, fechaRegistro = DateTime.Now, Rol_idRol = rol
        };

        db.Usuarios.AddRange(
            U(Admin, RolAdministrador), U(Auxiliar, RolAuxiliar), U(Docente, RolDocente), U(Tutor, RolTutor),
            U(Estudiante1, RolEstudiante), U(Estudiante2, RolEstudiante),
            // Igual que el seed de BD.sql: hash que NO es BCrypt.
            U(Legacy, RolDocente, "hash12345"));
        db.SaveChanges();
    }

    public static int IdDe(AgendamientoDbContext db, string correo) => db.Usuarios.Single(u => u.correo == correo).idUsuario;
}
