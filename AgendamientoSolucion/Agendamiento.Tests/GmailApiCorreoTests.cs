using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;
using AgendamientoGestion.Logica.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Agendamiento.Tests;

/// <summary>Transporte por la API de Gmail con un HttpMessageHandler falso (no sale nada a la red).</summary>
public class GmailApiCorreoTests
{
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string EnviarUrl = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    private sealed record Peticion(string Url, string? Autorizacion, string Cuerpo);

    /// <summary>Responde según la URL con respuestas encoladas; registra cada petición (cuerpo leído antes de liberarse).</summary>
    private sealed class HandlerFalso : HttpMessageHandler
    {
        public readonly List<Peticion> Peticiones = new();
        public readonly Queue<(HttpStatusCode, string)> Tokens = new();
        public readonly Queue<(HttpStatusCode, string)> Envios = new();

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cuerpo = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult() ?? "";
            var url = request.RequestUri!.ToString();
            Peticiones.Add(new Peticion(url, request.Headers.Authorization?.ToString(), cuerpo));
            var cola = url == TokenUrl ? Tokens : url == EnviarUrl ? Envios : throw new InvalidOperationException(url);
            var (estado, json) = cola.Count > 0 ? cola.Dequeue() : throw new InvalidOperationException($"Sin respuesta para {url}");
            return new HttpResponseMessage(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(Send(request, cancellationToken));

        public int Llamadas(string url) => Peticiones.Count(p => p.Url == url);
    }

    private static string Token(string t = "tok-1", int expira = 3599) =>
        JsonSerializer.Serialize(new { access_token = t, expires_in = expira, token_type = "Bearer" });

    private static readonly string EnvioOk = JsonSerializer.Serialize(new { id = "18c0ffee", threadId = "18c0ffee", labelIds = new[] { "SENT" } });

    private static GmailApiSettings Config() => new()
    {
        ClientId = "cliente.apps.googleusercontent.com", ClientSecret = "secreto-de-prueba", RefreshToken = "refresh-de-prueba",
        Reintentos = 2, EsperaBaseMilisegundos = 1
    };

    private static SmtpSettings Remitente() => new() { Remitente = "cuenta@gmail.com", ResponderA = "coordinacion@cecar.edu.co" };

    private static (GmailApiCorreoService Servicio, HandlerFalso Handler) Crear(GmailApiSettings? g = null, GmailAccessTokenCache? cache = null)
    {
        var handler = new HandlerFalso();
        var servicio = new GmailApiCorreoService(new HttpClient(handler), Options.Create(g ?? Config()), Options.Create(Remitente()),
            cache ?? new GmailAccessTokenCache(), NullLogger<GmailApiCorreoService>.Instance);
        return (servicio, handler);
    }

    private static MensajeCorreo Mensaje(string para = "estudiante@example.com") =>
        new(para, "Ana Pérez", "Nueva Tutoría Asignada - Inglés B1", "<p>Hola <strong>Ana</strong></p>", "Hola Ana");

    private static MimeMessage DecodificarRaw(string cuerpoJson)
    {
        var raw = JsonDocument.Parse(cuerpoJson).RootElement.GetProperty("raw").GetString()!;
        Assert.DoesNotContain('+', raw);
        Assert.DoesNotContain('/', raw);
        Assert.DoesNotContain('=', raw);
        var b64 = raw.Replace('-', '+').Replace('_', '/');
        b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
        return MimeMessage.Load(new MemoryStream(Convert.FromBase64String(b64)));
    }

    [Fact]
    public void Envio_correcto_pide_token_y_manda_el_mime_completo_en_base64url()
    {
        var (servicio, h) = Crear();
        h.Tokens.Enqueue((HttpStatusCode.OK, Token()));
        h.Envios.Enqueue((HttpStatusCode.OK, EnvioOk));

        var messageId = servicio.Enviar(Mensaje());

        var token = h.Peticiones.Single(p => p.Url == TokenUrl);
        Assert.Contains("grant_type=refresh_token", token.Cuerpo);
        Assert.Contains("refresh_token=refresh-de-prueba", token.Cuerpo);
        Assert.Contains("client_id=cliente.apps.googleusercontent.com", token.Cuerpo);

        var envio = h.Peticiones.Single(p => p.Url == EnviarUrl);
        Assert.Equal("Bearer tok-1", envio.Autorizacion);
        var mime = DecodificarRaw(envio.Cuerpo);
        Assert.Equal("cuenta@gmail.com", mime.From.Mailboxes.Single().Address);
        Assert.Equal("CDITutorPro – CECAR", mime.From.Mailboxes.Single().Name);
        Assert.Equal("estudiante@example.com", mime.To.Mailboxes.Single().Address);
        Assert.Equal("coordinacion@cecar.edu.co", mime.ReplyTo.Mailboxes.Single().Address);
        Assert.Equal("Nueva Tutoría Asignada - Inglés B1", mime.Subject);
        Assert.Equal(messageId, mime.MessageId);
        Assert.Equal("auto-generated", mime.Headers["Auto-Submitted"]);
        Assert.Equal("Hola Ana", mime.TextBody.Trim());
        Assert.Contains("<strong>Ana</strong>", mime.HtmlBody);
    }

    [Fact]
    public void El_access_token_se_cachea_entre_envios()
    {
        var cache = new GmailAccessTokenCache();
        var (servicio, h) = Crear(cache: cache);
        h.Tokens.Enqueue((HttpStatusCode.OK, Token()));
        h.Envios.Enqueue((HttpStatusCode.OK, EnvioOk));
        h.Envios.Enqueue((HttpStatusCode.OK, EnvioOk));

        servicio.Enviar(Mensaje());
        servicio.Enviar(Mensaje("otro@example.com"));

        Assert.Equal(1, h.Llamadas(TokenUrl));
        Assert.Equal(2, h.Llamadas(EnviarUrl));
    }

    [Fact]
    public void Token_a_punto_de_caducar_se_renueva()
    {
        var (servicio, h) = Crear();
        h.Tokens.Enqueue((HttpStatusCode.OK, Token("corto", expira: 60))); // < margen de 2 min
        h.Tokens.Enqueue((HttpStatusCode.OK, Token("nuevo")));
        h.Envios.Enqueue((HttpStatusCode.OK, EnvioOk));
        h.Envios.Enqueue((HttpStatusCode.OK, EnvioOk));

        servicio.Enviar(Mensaje());
        servicio.Enviar(Mensaje());

        Assert.Equal(2, h.Llamadas(TokenUrl));
        Assert.Equal("Bearer nuevo", h.Peticiones.Last().Autorizacion);
    }

    [Fact]
    public void Respuesta_401_renueva_el_token_y_reintenta_una_sola_vez()
    {
        var (servicio, h) = Crear(new GmailApiSettings
        {
            ClientId = "c", ClientSecret = "s", RefreshToken = "r", Reintentos = 0, EsperaBaseMilisegundos = 1
        });
        h.Tokens.Enqueue((HttpStatusCode.OK, Token("viejo")));
        h.Tokens.Enqueue((HttpStatusCode.OK, Token("nuevo")));
        h.Envios.Enqueue((HttpStatusCode.Unauthorized, """{"error":{"code":401,"status":"UNAUTHENTICATED"}}"""));
        h.Envios.Enqueue((HttpStatusCode.OK, EnvioOk));

        servicio.Enviar(Mensaje()); // con Reintentos = 0: el reintento por 401 no consume reintentos

        Assert.Equal(new[] { "Bearer viejo", "Bearer nuevo" }, h.Peticiones.Where(p => p.Url == EnviarUrl).Select(p => p.Autorizacion));

        // Un segundo 401 seguido ya no se reintenta: falla.
        var (servicio2, h2) = Crear();
        h2.Tokens.Enqueue((HttpStatusCode.OK, Token("a")));
        h2.Tokens.Enqueue((HttpStatusCode.OK, Token("b")));
        h2.Envios.Enqueue((HttpStatusCode.Unauthorized, "{}"));
        h2.Envios.Enqueue((HttpStatusCode.Unauthorized, "{}"));
        var ex = Assert.Throws<GmailApiException>(() => servicio2.Enviar(Mensaje()));
        Assert.Equal(HttpStatusCode.Unauthorized, ex.Estado);
        Assert.Equal(2, h2.Llamadas(EnviarUrl));
    }

    [Fact]
    public void Invalid_grant_falla_sin_reintentar_y_sin_exponer_secretos()
    {
        var (servicio, h) = Crear();
        h.Tokens.Enqueue((HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"Token has been expired or revoked."}"""));

        var ex = Assert.Throws<GmailApiException>(() => servicio.Enviar(Mensaje()));

        Assert.False(ex.Transitorio);
        Assert.Contains("revocado o caducado", ex.Message);
        Assert.DoesNotContain("refresh-de-prueba", ex.Message);
        Assert.DoesNotContain("secreto-de-prueba", ex.Message);
        Assert.Equal(1, h.Llamadas(TokenUrl));
        Assert.Equal(0, h.Llamadas(EnviarUrl));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public void Errores_transitorios_se_reintentan(HttpStatusCode estado)
    {
        var (servicio, h) = Crear();
        h.Tokens.Enqueue((HttpStatusCode.OK, Token()));
        h.Envios.Enqueue((estado, """{"error":{"code":500,"status":"BACKEND_ERROR","message":"Backend Error"}}"""));
        h.Envios.Enqueue((estado, "{}"));
        h.Envios.Enqueue((HttpStatusCode.OK, EnvioOk));

        servicio.Enviar(Mensaje());

        Assert.Equal(3, h.Llamadas(EnviarUrl));
        Assert.Equal(1, h.Llamadas(TokenUrl));
    }

    [Fact]
    public void Transitorio_persistente_agota_los_reintentos_y_lanza()
    {
        var (servicio, h) = Crear();
        h.Tokens.Enqueue((HttpStatusCode.OK, Token()));
        for (var i = 0; i < 3; i++) h.Envios.Enqueue((HttpStatusCode.BadGateway, "{}"));

        var ex = Assert.Throws<GmailApiException>(() => servicio.Enviar(Mensaje()));
        Assert.True(ex.Transitorio);
        Assert.Equal(3, h.Llamadas(EnviarUrl)); // 1 + Reintentos (2)
    }

    [Fact]
    public void Error_400_de_gmail_no_se_reintenta_y_200_sin_id_tampoco_cuenta_como_enviado()
    {
        var (servicio, h) = Crear();
        h.Tokens.Enqueue((HttpStatusCode.OK, Token()));
        h.Envios.Enqueue((HttpStatusCode.BadRequest, """{"error":{"code":400,"status":"INVALID_ARGUMENT","message":"Invalid To header"}}"""));
        var ex = Assert.Throws<GmailApiException>(() => servicio.Enviar(Mensaje()));
        Assert.Contains("INVALID_ARGUMENT", ex.Message);
        Assert.Equal(1, h.Llamadas(EnviarUrl));

        h.Envios.Enqueue((HttpStatusCode.OK, "{}"));
        Assert.Throws<GmailApiException>(() => servicio.Enviar(Mensaje()));
    }

    [Fact]
    public void Configuracion_incompleta_falla_antes_de_llamar_a_google_con_mensaje_claro()
    {
        var (servicio, h) = Crear(new GmailApiSettings { ClientId = "c" });
        var ex = Assert.Throws<InvalidOperationException>(() => servicio.Enviar(Mensaje()));
        Assert.Contains("GmailApi:ClientSecret", ex.Message);
        Assert.Contains("GmailApi:RefreshToken", ex.Message);
        Assert.DoesNotContain("GmailApi:ClientId", ex.Message);
        Assert.Empty(h.Peticiones);

        var sinRemitente = Assert.Throws<InvalidOperationException>(() =>
            GmailApiCorreoService.ValidarConfiguracion(Config(), new SmtpSettings()));
        Assert.Contains("SmtpSettings:Remitente", sinRemitente.Message);
    }

    [Fact]
    public void Proveedor_invalido_se_rechaza_y_por_defecto_es_smtp()
    {
        Assert.Equal("Smtp", new CorreoSettings { Proveedor = "" }.ProveedorValidado());
        Assert.Equal("GmailApi", new CorreoSettings { Proveedor = "gmailapi" }.ProveedorValidado());
        Assert.Throws<InvalidOperationException>(() => new CorreoSettings { Proveedor = "Sendgrid" }.ProveedorValidado());
    }

    // ------------------------------------------------------------------ arranque de la API

    private sealed class ApiConGmailIncompleto : ApiFactory
    {
        protected override IDictionary<string, string?> Configuracion
        {
            get
            {
                var c = base.Configuracion;
                c["Correo:Proveedor"] = "GmailApi";
                c["GmailApi:ClientId"] = "solo-el-id";
                c["SmtpSettings:Remitente"] = "cuenta@gmail.com";
                return c;
            }
        }
    }

    [Fact]
    public void La_api_no_arranca_con_GmailApi_sin_credenciales()
    {
        using var f = new ApiConGmailIncompleto();
        var ex = Assert.ThrowsAny<Exception>(() => f.CreateClient());
        var mensaje = ex is InvalidOperationException ? ex.Message : ex.ToString();
        Assert.Contains("GmailApi:ClientSecret", mensaje);
        Assert.Contains("GmailApi:RefreshToken", mensaje);
    }
}
