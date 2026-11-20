using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Logica.Seguridad;
using AgendamientoGestion.Logica.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Agendamiento.Tests;

/// <summary>"¿Olvidaste tu contraseña?": solicitud, validación y restablecimiento (contrato en docs/api/endpoints.md).</summary>
public class RestablecimientoContrasenaTests : IClassFixture<ApiFactory>
{
    private const string NuevaClave = "OtraClave-2026";
    private readonly ApiFactory _f;

    public RestablecimientoContrasenaTests(ApiFactory factory) => _f = factory;

    private async Task<HttpResponseMessage> Solicitar(string correo)
    {
        using var c = _f.CreateClient();
        return await c.PostAsJsonAsync("/api/Auth/olvide-contrasena", new { correo });
    }

    private async Task<FakeCorreoService.Correo> EsperarCorreo(string destinatario, string asunto, int previos = 0)
    {
        for (var i = 0; i < 100; i++)
        {
            var lista = _f.Correo.Enviados.Where(c => c.Destinatario == destinatario && c.Asunto == asunto).ToList();
            if (lista.Count > previos) return lista.Last();
            await Task.Delay(50);
        }
        throw new TimeoutException($"No llegó '{asunto}' a {destinatario}");
    }

    private static string Token(FakeCorreoService.Correo correo)
    {
        var m = Regex.Match(correo.Texto, @"restablecer-contrasena\?token=([A-Za-z0-9_\-]+)");
        Assert.True(m.Success, "El correo no contiene el enlace");
        return m.Groups[1].Value;
    }

    private async Task<HttpResponseMessage> Validar(string token)
    {
        using var c = _f.CreateClient();
        return await c.GetAsync($"/api/Auth/restablecer-contrasena/validar?token={Uri.EscapeDataString(token)}");
    }

    private async Task<HttpResponseMessage> Restablecer(string token, string clave)
    {
        using var c = _f.CreateClient();
        return await c.PostAsJsonAsync("/api/Auth/restablecer-contrasena", new { token, contrasenaNueva = clave });
    }

    private async Task<HttpResponseMessage> Login(string correo, string clave)
    {
        using var c = _f.CreateClient();
        return await c.PostAsJsonAsync("/api/Auth/login", new { correo, contrasena = clave });
    }

    [Fact]
    public async Task Flujo_completo_token_de_un_solo_uso_e_invalida_sesiones_previas()
    {
        const string correo = "olvido.docente@test.com";
        var id = Datos.CrearUsuario(_f, correo, Seed.RolDocente);
        var tokenSesion = (await (await Login(correo, ApiFactory.Password)).JsonAsync()).Str("token");

        var r = await Solicitar(correo);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(RestablecimientoContrasenaService.MensajeSolicitud, (await r.JsonAsync()).Str("message"));

        var correoRecibido = await EsperarCorreo(correo, "Restablece tu contraseña");
        Assert.Contains($"{ApiFactory.UrlWebDePrueba}/restablecer-contrasena?token=", correoRecibido.Html);
        Assert.Contains("caduca en 60 minutos", correoRecibido.Texto);
        Assert.Contains("ignora este correo", correoRecibido.Texto);
        var token = Token(correoRecibido);
        Assert.True(token.Length >= 43); // 32 bytes en base64url

        // En BD solo el hash.
        Assert.True(_f.ConDb(db => db.RestablecimientosContrasena.Any(x => x.Usuario_idUsuario == id
            && x.tokenHash == RestablecimientoContrasenaService.Hash(token))));
        Assert.False(_f.ConDb(db => db.RestablecimientosContrasena.Any(x => x.tokenHash == token)));

        var v = await Validar(token);
        Assert.Equal(HttpStatusCode.OK, v.StatusCode);
        var jv = await v.JsonAsync();
        Assert.True(jv["valido"]!.GetValue<bool>());
        Assert.Equal("o***@test.com", jv.Str("correo"));
        Assert.EndsWith("Z", jv["expiraEn"]!.GetValue<string>());

        // La sesión anterior funciona hasta el restablecimiento.
        using var conSesion = _f.CreateClient();
        conSesion.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenSesion);
        Assert.Equal(HttpStatusCode.OK, (await conSesion.GetAsync("/api/Auth/me")).StatusCode);

        await Task.Delay(1100); // el iat del JWT tiene resolución de segundos
        Assert.Equal(HttpStatusCode.NoContent, (await Restablecer(token, NuevaClave)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Login(correo, NuevaClave)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(correo, ApiFactory.Password)).StatusCode);
        var me = await conSesion.GetAsync("/api/Auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Contains("se cambió", (await me.JsonAsync()).Str("message"));
        // Un token emitido después sí vale.
        var nuevoToken = (await (await Login(correo, NuevaClave)).JsonAsync()).Str("token");
        using var c2 = _f.CreateClient();
        c2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nuevoToken);
        Assert.Equal(HttpStatusCode.OK, (await c2.GetAsync("/api/Auth/me")).StatusCode);

        // Un solo uso.
        var reuso = await Restablecer(token, "Tercera-Clave-1");
        Assert.Equal(HttpStatusCode.BadRequest, reuso.StatusCode);
        Assert.Equal(RestablecimientoContrasenaService.MensajeTokenInvalido, (await reuso.JsonAsync()).Str("message"));
        Assert.Equal(HttpStatusCode.BadRequest, (await Validar(token)).StatusCode);

        await EsperarCorreo(correo, "Tu contraseña se cambió");
    }

    [Fact]
    public async Task Misma_respuesta_y_sin_correo_para_inexistentes_estudiantes_e_inactivos()
    {
        var est = Datos.CrearUsuario(_f, "olvido.est@test.com", Seed.RolEstudiante);
        var inactivo = Datos.CrearUsuario(_f, "olvido.inactivo@test.com", Seed.RolTutor);
        _f.ConDb(db => { db.Usuarios.Single(u => u.idUsuario == inactivo).activo = false; return db.SaveChanges(); });
        var testigo = "olvido.testigo@test.com";
        Datos.CrearUsuario(_f, testigo, Seed.RolTutor);

        var cuerpos = new List<string>();
        foreach (var correo in new[] { "nadie.existe@test.com", "olvido.est@test.com", "olvido.inactivo@test.com", "OLVIDO.TESTIGO@test.com" })
        {
            var r = await Solicitar(correo);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            cuerpos.Add(await r.Content.ReadAsStringAsync());
        }
        Assert.Single(cuerpos.Distinct());

        // La cola se procesa en orden: cuando llega el del testigo, los anteriores ya se procesaron.
        await EsperarCorreo(testigo, "Restablece tu contraseña");
        Assert.DoesNotContain(_f.Correo.Enviados, c => c.Destinatario is "nadie.existe@test.com" or "olvido.est@test.com" or "olvido.inactivo@test.com");
        Assert.False(_f.ConDb(db => db.RestablecimientosContrasena.Any(x => x.Usuario_idUsuario == est || x.Usuario_idUsuario == inactivo)));
    }

    [Fact]
    public async Task Una_solicitud_nueva_invalida_la_anterior_y_el_caducado_no_vale()
    {
        const string correo = "olvido.doble@test.com";
        var id = Datos.CrearUsuario(_f, correo, Seed.RolTutor);
        await Solicitar(correo);
        var primero = Token(await EsperarCorreo(correo, "Restablece tu contraseña"));
        await Solicitar(correo);
        var segundo = Token(await EsperarCorreo(correo, "Restablece tu contraseña", previos: 1));

        Assert.NotEqual(primero, segundo);
        Assert.Equal(HttpStatusCode.BadRequest, (await Validar(primero)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Validar(segundo)).StatusCode);

        const string caducado = "token-caducado-de-prueba-0123456789abcdefghijk";
        _f.ConDb(db =>
        {
            db.RestablecimientosContrasena.Add(new RestablecimientoContrasena
            {
                Usuario_idUsuario = id, tokenHash = RestablecimientoContrasenaService.Hash(caducado),
                fechaCreacion = DateTime.UtcNow.AddHours(-2), fechaExpiracion = DateTime.UtcNow.AddMinutes(-1)
            });
            return db.SaveChanges();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await Validar(caducado)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Restablecer(caducado, NuevaClave)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Validar("inventado")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Validar("")).StatusCode);
    }

    [Fact]
    public async Task Politica_de_contrasena_igual_que_el_cambio_y_no_consume_el_token()
    {
        const string correo = "olvido.politica@test.com";
        Datos.CrearUsuario(_f, correo, Seed.RolDocente);
        await Solicitar(correo);
        var token = Token(await EsperarCorreo(correo, "Restablece tu contraseña"));

        async Task Rechaza(string clave, string mensaje)
        {
            var r = await Restablecer(token, clave);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal(mensaje, (await r.JsonAsync()).Str("message"));
        }

        await Rechaza("corta", PoliticaContrasena.MensajeLongitud);
        await Rechaza(new string('a', 73), PoliticaContrasena.MensajeLongitud);
        await Rechaza("          ", PoliticaContrasena.MensajeSoloEspacios);
        await Rechaza(ApiFactory.Password, PoliticaContrasena.MensajeIgualActual);
        await Rechaza(correo.ToUpperInvariant(), PoliticaContrasena.MensajeIgualCorreo);

        Assert.Equal(HttpStatusCode.NoContent, (await Restablecer(token, NuevaClave)).StatusCode);
    }

    [Fact]
    public async Task Endpoint_de_desarrollo_del_enlace_no_existe_fuera_de_Development()
    {
        var correo = "prueba.dev.enlace@test.com";
        Datos.CrearUsuario(_f, correo, Seed.RolDocente);
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync("/dev/restablecimiento/enlace", new { correo })).StatusCode);
        using var anonimo = _f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync("/dev/restablecimiento/enlace", new { correo })).StatusCode);
        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.PostAsJsonAsync("/dev/restablecimiento/enlace", new { correo })).StatusCode);
    }

    [Fact]
    public async Task Correo_no_valido_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Solicitar("no-es-correo")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Solicitar("")).StatusCode);
    }
}

/// <summary>Límites bajos para probar el 429.</summary>
public class ApiFactoryLimitesRestablecimiento : ApiFactory
{
    protected override IDictionary<string, string?> Configuracion
    {
        get
        {
            var c = base.Configuracion;
            c["RestablecimientoContrasena:SolicitudesPorCorreo"] = "2";
            c["RestablecimientoContrasena:SolicitudesPorIp"] = "4";
            c["RestablecimientoContrasena:IntentosRestablecerPorIp"] = "3";
            return c;
        }
    }
}

public class RestablecimientoLimitesTests : IClassFixture<ApiFactoryLimitesRestablecimiento>
{
    private readonly ApiFactoryLimitesRestablecimiento _f;
    public RestablecimientoLimitesTests(ApiFactoryLimitesRestablecimiento f) => _f = f;

    [Fact]
    public async Task Rate_limit_por_correo_por_ip_y_en_restablecer()
    {
        using var c = _f.CreateClient();
        Task<HttpResponseMessage> Pedir(string correo) => c.PostAsJsonAsync("/api/Auth/olvide-contrasena", new { correo });

        Assert.Equal(HttpStatusCode.OK, (await Pedir("limite@test.com")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Pedir("LIMITE@test.com")).StatusCode);
        var porCorreo = await Pedir("limite@test.com");
        Assert.Equal((HttpStatusCode)429, porCorreo.StatusCode);
        Assert.True(porCorreo.Headers.RetryAfter?.Delta?.TotalSeconds > 0);
        Assert.False(string.IsNullOrWhiteSpace((await porCorreo.JsonAsync()).Str("message")));

        Assert.Equal(HttpStatusCode.OK, (await Pedir("otro1@test.com")).StatusCode);
        var porIp = await Pedir("otro2@test.com"); // 5.ª petición de esta IP (límite 4)
        Assert.Equal((HttpStatusCode)429, porIp.StatusCode);
        Assert.NotNull(porIp.Headers.RetryAfter);

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/Auth/restablecer-contrasena/validar?token=x")).StatusCode);
        Assert.Equal((HttpStatusCode)429, (await c.PostAsJsonAsync("/api/Auth/restablecer-contrasena", new { token = "x", contrasenaNueva = "Algo-12345" })).StatusCode);
    }
}

/// <summary>El Auxiliar crea y edita Estudiantes, Docentes y Tutores; nunca Administradores ni Auxiliares.</summary>
public class AuxiliarGestionPersonalTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public AuxiliarGestionPersonalTests(ApiFactory f) => _f = f;

    private static object Nuevo(string correo, int rolId) => new
    {
        nombres = "Aux", apellidos = "Gestiona", correo, contrasena = "Secreta123", rolId
    };

    [Fact]
    public async Task Auxiliar_crea_y_edita_docentes_y_tutores_pero_no_admins_ni_auxiliares()
    {
        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);

        var docente = await aux.PostAsJsonAsync("/api/Usuario", Nuevo("aux.docente@test.com", Seed.RolDocente));
        Assert.Equal(HttpStatusCode.Created, docente.StatusCode);
        var docenteId = (await docente.JsonAsync())["idUsuario"]!.GetValue<int>();
        var tutor = await aux.PostAsJsonAsync("/api/Usuario", Nuevo("aux.tutor@test.com", Seed.RolTutor));
        Assert.Equal(HttpStatusCode.Created, tutor.StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await aux.PostAsJsonAsync("/api/Usuario", Nuevo("aux.est@test.com", Seed.RolEstudiante))).StatusCode);

        // Editar docente (datos) y cambiarlo a Tutor: permitido.
        var put = await aux.PutAsJsonAsync($"/api/Usuario/{docenteId}", new { nombres = "Editado", apellidos = "Por Aux", correo = "aux.docente@test.com", rolId = Seed.RolTutor });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Tutor", (await put.JsonAsync()).Str("tipoRol"));

        // Crear / promover / editar Administrador o Auxiliar: 403.
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.PostAsJsonAsync("/api/Usuario", Nuevo("aux.admin@test.com", Seed.RolAdministrador))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.PostAsJsonAsync("/api/Usuario", Nuevo("aux.aux@test.com", Seed.RolAuxiliar))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.PutAsJsonAsync($"/api/Usuario/{docenteId}", new { nombres = "X", apellidos = "Y", correo = "aux.docente@test.com", rolId = Seed.RolAdministrador })).StatusCode);
        var adminId = _f.ConDb(db => Seed.IdDe(db, Seed.Admin));
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.PutAsJsonAsync($"/api/Usuario/{adminId}", new { nombres = "X", apellidos = "Y", correo = Seed.Admin, rolId = Seed.RolAdministrador })).StatusCode);
        Assert.False(_f.ConDb(db => db.Usuarios.Any(u => u.correo == "aux.admin@test.com" || u.correo == "aux.aux@test.com")));
        Assert.Equal(Seed.RolTutor, _f.ConDb(db => db.Usuarios.Single(u => u.idUsuario == docenteId).Rol_idRol));

        // Eliminar y desactivar personal siguen reservados al Administrador.
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.DeleteAsync($"/api/Usuario/{docenteId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.PatchAsJsonAsync($"/api/Usuario/{docenteId}/estado", new { activo = false })).StatusCode);
    }
}
