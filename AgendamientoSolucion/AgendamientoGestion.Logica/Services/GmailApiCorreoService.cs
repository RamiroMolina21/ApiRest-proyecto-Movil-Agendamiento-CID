using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AgendamientoGestion.Logica.Services
{
    /// <summary>Error de la API de Gmail o del endpoint de tokens de Google. Nunca contiene secretos.</summary>
    public sealed class GmailApiException : Exception
    {
        public GmailApiException(string mensaje, HttpStatusCode? estado, bool transitorio) : base(mensaje)
        {
            Estado = estado;
            Transitorio = transitorio;
        }

        public HttpStatusCode? Estado { get; }
        public bool Transitorio { get; }
    }

    /// <summary>
    /// Caché del access token (singleton, thread-safe). Un único token para la cuenta configurada; se renueva bajo
    /// bloqueo para que peticiones concurrentes no pidan varios tokens a la vez.
    /// </summary>
    public sealed class GmailAccessTokenCache
    {
        /// <summary>Se renueva este margen antes de que caduque (evita usar un token que expira en vuelo).</summary>
        public static readonly TimeSpan Margen = TimeSpan.FromMinutes(2);

        private readonly object _bloqueo = new();
        private string? _token;
        private DateTimeOffset _expira;

        public string Obtener(Func<(string Token, TimeSpan Vigencia)> renovar, bool forzar = false)
        {
            lock (_bloqueo)
            {
                if (!forzar && _token != null && DateTimeOffset.UtcNow < _expira - Margen) return _token;
                var (token, vigencia) = renovar();
                _token = token;
                _expira = DateTimeOffset.UtcNow + vigencia;
                return token;
            }
        }

        public void Invalidar()
        {
            lock (_bloqueo) { _token = null; }
        }
    }

    /// <summary>
    /// Transporte por la API de Gmail (HTTPS 443) para hosts que bloquean SMTP (Render gratuito). Mismo MIME que el
    /// SMTP (CorreoService.ConstruirMensaje): multipart texto+HTML, From/Reply-To/Message-ID/cabeceras. OAuth2 con
    /// refresh token → access token cacheado. Reintenta 429/5xx/red con espera creciente; ante 401 renueva el token y
    /// reintenta una vez. Devuelve el Message-ID solo si Gmail responde 200 con id (si no, lanza: la Notificacion no se
    /// guarda). Ver docs/correo/gmail-api.md.
    /// </summary>
    public class GmailApiCorreoService : ICorreoService
    {
        private readonly HttpClient _http;
        private readonly GmailApiSettings _gmail;
        private readonly SmtpSettings _remitente;
        private readonly GmailAccessTokenCache _cache;
        private readonly ILogger<GmailApiCorreoService> _logger;

        public GmailApiCorreoService(HttpClient http, IOptions<GmailApiSettings> gmail, IOptions<SmtpSettings> remitente,
            GmailAccessTokenCache cache, ILogger<GmailApiCorreoService> logger)
        {
            _http = http;
            _gmail = gmail.Value;
            _remitente = remitente.Value;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>Lanza con un mensaje claro si falta configuración (se llama al arrancar y antes de cada envío).</summary>
        public static void ValidarConfiguracion(GmailApiSettings g, SmtpSettings remitente)
        {
            var faltan = new List<string>();
            if (string.IsNullOrWhiteSpace(g.ClientId)) faltan.Add("GmailApi:ClientId");
            if (string.IsNullOrWhiteSpace(g.ClientSecret)) faltan.Add("GmailApi:ClientSecret");
            if (string.IsNullOrWhiteSpace(g.RefreshToken)) faltan.Add("GmailApi:RefreshToken");
            if (string.IsNullOrWhiteSpace(remitente.DireccionRemitente)) faltan.Add("SmtpSettings:Remitente (o SmtpSettings:User)");
            if (faltan.Count > 0)
                throw new InvalidOperationException(
                    $"Correo:Proveedor = GmailApi pero falta configuración: {string.Join(", ", faltan)}. " +
                    "En Render: variables de entorno GmailApi__ClientId, GmailApi__ClientSecret, GmailApi__RefreshToken y SmtpSettings__Remitente. Ver docs/correo/gmail-api.md.");
        }

        public string Enviar(MensajeCorreo mensaje)
        {
            ValidarConfiguracion(_gmail, _remitente);
            using var mime = CorreoService.ConstruirMensaje(_remitente, mensaje);
            var raw = CodificarRaw(mime);

            var intentos = Math.Max(0, _gmail.Reintentos) + 1;
            var tokenRenovadoPor401 = false;
            for (var intento = 1; ; intento++)
            {
                try
                {
                    var token = _cache.Obtener(PedirToken);
                    var (estado, cuerpo) = PostJson(_gmail.EnviarUrl, token, raw);

                    if (estado == HttpStatusCode.OK)
                    {
                        var id = LeerPropiedad(cuerpo, "id");
                        if (string.IsNullOrWhiteSpace(id))
                            throw new GmailApiException("Gmail respondió 200 sin id de mensaje", estado, transitorio: false);
                        _logger.LogInformation("Correo enviado {MessageId} a @{Dominio} por la API de Gmail (id {GmailId}, intento {Intento})",
                            mime.MessageId, Dominio(mensaje.Destinatario), id, intento);
                        return mime.MessageId;
                    }

                    if (estado == HttpStatusCode.Unauthorized && !tokenRenovadoPor401)
                    {
                        // Token revocado o caducado antes de tiempo: renovar y reintentar UNA vez (no consume reintentos).
                        _logger.LogWarning("La API de Gmail respondió 401 para {MessageId}: se renueva el access token y se reintenta", mime.MessageId);
                        _cache.Invalidar();
                        tokenRenovadoPor401 = true;
                        intento--;
                        continue;
                    }

                    throw new GmailApiException($"La API de Gmail respondió {(int)estado}: {DescribirError(cuerpo)}", estado,
                        transitorio: EsEstadoTransitorio(estado));
                }
                catch (Exception ex) when (EsTransitorio(ex) && intento < intentos)
                {
                    var espera = Espera(intento);
                    _logger.LogWarning(ex, "Fallo transitorio enviando {MessageId} por la API de Gmail (intento {Intento}/{Total}); reintento en {Espera} ms",
                        mime.MessageId, intento, intentos, espera.TotalMilliseconds);
                    Thread.Sleep(espera);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo enviar el correo {MessageId} a @{Dominio} por la API de Gmail tras {Intento} intento(s)",
                        mime.MessageId, Dominio(mensaje.Destinatario), intento);
                    throw;
                }
            }
        }

        /// <summary>POST oauth2.googleapis.com/token (grant_type=refresh_token). Nunca registra los valores enviados ni el token.</summary>
        private (string Token, TimeSpan Vigencia) PedirToken()
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, _gmail.TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = _gmail.ClientId.Trim(),
                    ["client_secret"] = _gmail.ClientSecret.Trim(),
                    ["refresh_token"] = _gmail.RefreshToken.Trim()
                })
            };
            var (estado, cuerpo) = Ejecutar(peticion);

            if (estado == HttpStatusCode.OK)
            {
                var token = LeerPropiedad(cuerpo, "access_token");
                if (string.IsNullOrWhiteSpace(token))
                    throw new GmailApiException("Google devolvió 200 sin access_token", estado, transitorio: false);
                var segundos = int.TryParse(LeerPropiedad(cuerpo, "expires_in"), out var s) && s > 0 ? s : 3600;
                return (token, TimeSpan.FromSeconds(segundos));
            }

            var error = LeerPropiedad(cuerpo, "error") ?? "desconocido";
            if (error == "invalid_grant")
            {
                _logger.LogError("Google rechazó el refresh token de Gmail (invalid_grant): refresh token revocado o caducado. " +
                                 "Genera uno nuevo (docs/correo/gmail-api.md) y actualiza GmailApi__RefreshToken.");
                throw new GmailApiException("Refresh token de Gmail revocado o caducado (invalid_grant)", estado, transitorio: false);
            }

            throw new GmailApiException($"No se pudo obtener el access token de Gmail ({(int)estado}, error '{error}')", estado,
                transitorio: EsEstadoTransitorio(estado));
        }

        private (HttpStatusCode, string) PostJson(string url, string token, string raw)
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { raw }), Encoding.UTF8, "application/json")
            };
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return Ejecutar(peticion);
        }

        private (HttpStatusCode, string) Ejecutar(HttpRequestMessage peticion)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, _gmail.TimeoutSegundos)));
            using var respuesta = _http.Send(peticion, cts.Token);
            using var lector = new StreamReader(respuesta.Content.ReadAsStream(cts.Token));
            return (respuesta.StatusCode, lector.ReadToEnd());
        }

        /// <summary>MIME completo (RFC 5322) en base64url sin relleno, como exige el campo "raw" de messages.send.</summary>
        public static string CodificarRaw(MimeMessage mime)
        {
            using var ms = new MemoryStream();
            mime.WriteTo(FormatOptions.Default, ms);
            return Convert.ToBase64String(ms.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private TimeSpan Espera(int intento)
        {
            double[] factores = { 1, 2.5, 5 };
            return TimeSpan.FromMilliseconds(Math.Max(0, _gmail.EsperaBaseMilisegundos) * factores[Math.Min(intento - 1, factores.Length - 1)]);
        }

        private static bool EsEstadoTransitorio(HttpStatusCode estado) =>
            estado == HttpStatusCode.TooManyRequests || (int)estado >= 500;

        private static bool EsTransitorio(Exception ex) => ex switch
        {
            GmailApiException g => g.Transitorio,
            HttpRequestException or IOException or TimeoutException => true,
            OperationCanceledException => true, // timeout del CancellationTokenSource
            _ => false
        };

        private static string? LeerPropiedad(string json, string nombre)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(nombre, out var v)
                    ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>Resumen del error de Gmail ({"error":{"status","message"}}), sin cuerpos largos.</summary>
        private static string DescribirError(string cuerpo)
        {
            try
            {
                using var doc = JsonDocument.Parse(cuerpo);
                if (doc.RootElement.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.Object)
                {
                    var status = e.TryGetProperty("status", out var s) ? s.GetString() : null;
                    var msg = e.TryGetProperty("message", out var m) ? m.GetString() : null;
                    return Recortar($"{status} {msg}".Trim());
                }
            }
            catch (JsonException) { }
            return Recortar(cuerpo);
        }

        private static string Recortar(string s) => s.Length <= 300 ? s : s[..300] + "…";

        private static string Dominio(string correo)
        {
            var i = correo.LastIndexOf('@');
            return i >= 0 ? correo[(i + 1)..] : "?";
        }
    }
}
