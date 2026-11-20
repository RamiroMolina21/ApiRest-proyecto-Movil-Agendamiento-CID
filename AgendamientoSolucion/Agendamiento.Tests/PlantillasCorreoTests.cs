using AgendamientoGestion.Logica.Correo;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;
using AgendamientoGestion.Logica.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Agendamiento.Tests;

/// <summary>
/// Plantillas de correo y construcción MIME. Además deja una PREVISUALIZACIÓN de cada plantilla en
/// {salida de tests}/correos-preview/*.html|.txt (ruta en la salida del test) para revisar el diseño sin enviar.
/// </summary>
public class PlantillasCorreoTests
{
    private static readonly SmtpSettings Smtp = new()
    {
        Remitente = "notificaciones@cecar.test", NombreRemitente = "CDITutorPro – CECAR", ResponderA = "coordinacion@cecar.test",
        User = "notificaciones@cecar.test", Password = "x", Host = "smtp.invalid", Port = 587, EnableSsl = true
    };

    private static PlantillasCorreo Plantillas() => new(Options.Create(Smtp));

    [Fact]
    public void Cada_plantilla_de_ejemplo_se_renderiza_y_se_exporta_para_previsualizar()
    {
        var carpeta = Path.Combine(AppContext.BaseDirectory, "correos-preview");
        Directory.CreateDirectory(carpeta);
        var plantillas = Plantillas();

        var ejemplos = EjemplosCorreo.Todos();
        // Toda plantilla incrustada tiene ejemplo (y viceversa): obliga a mantener la previsualización al día.
        var incrustadas = PlantillasCorreo.Disponibles().Where(n => !n.StartsWith('_')).Select(Path.GetFileNameWithoutExtension).Distinct().OrderBy(x => x);
        Assert.Equal(incrustadas, ejemplos.Select(e => e.Plantilla).OrderBy(x => x));

        foreach (var modelo in ejemplos)
        {
            var c = plantillas.Renderizar(modelo);
            Assert.False(string.IsNullOrWhiteSpace(c.Asunto));
            Assert.True(c.Asunto.Length <= 45, $"El asunto de {modelo.Plantilla} no cabe en Notificacion.asunto (45)");
            Assert.StartsWith("<!DOCTYPE html>", c.Html.TrimStart());
            Assert.DoesNotContain("{{", c.Html);
            Assert.DoesNotContain("{{", c.Texto);
            Assert.DoesNotContain("<", c.Texto);
            Assert.Contains("María José", c.Html);
            Assert.Contains("María José", c.Texto);
            Assert.Contains(PlantillasCorreo.NombreSistemaPorDefecto, c.Texto);
            Assert.DoesNotContain("localhost", c.Html);

            File.WriteAllText(Path.Combine(carpeta, $"{modelo.Plantilla}.html"), c.Html);
            File.WriteAllText(Path.Combine(carpeta, $"{modelo.Plantilla}.txt"), $"Asunto: {c.Asunto}\r\n\r\n{c.Texto}");
        }
    }

    [Fact]
    public void Los_valores_se_escapan_y_no_se_interpretan_como_plantilla()
    {
        var c = Plantillas().Renderizar(new NuevaTutoriaCorreo(
            "<script>alert(1)</script> O'Brien & \"Co\"", 7, "Inglés", "B1", "{{idTutoria}} {{{contenido}}}", "Virtual",
            new DateTime(2026, 11, 20, 8, 0, 0), "<b>Aula</b>"));

        Assert.DoesNotContain("<script>", c.Html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; O&#x27;Brien &amp; &quot;Co&quot;", c.Html);
        Assert.Contains("&lt;b&gt;Aula&lt;/b&gt;", c.Html);
        Assert.Contains("{{idTutoria}} {{{contenido}}}", c.Html); // literal, no inyecta
        Assert.Contains("<script>alert(1)</script> O'Brien", c.Texto); // el texto plano no lleva HTML-encoding
        Assert.Contains("mailto:coordinacion@cecar.test", c.Html); // Reply-To como contacto, no direcciones inventadas
    }

    [Fact]
    public void Cambio_sin_cambios_ni_motivo_usa_las_secciones_alternativas()
    {
        var c = Plantillas().Renderizar(new CambioTutoriaCorreo("Ana", 1, DateTime.Today, "Inglés", "A1", "T", "Virtual",
            null, null, null, Array.Empty<string>()));
        Assert.Contains("No se detectaron cambios", c.Html);
        Assert.DoesNotContain("Motivo del cambio", c.Html);
        Assert.Contains("Sin asignar", c.Html);
    }

    [Fact]
    public void Motor_rechaza_variables_inexistentes_y_triple_llave_no_segura()
    {
        Assert.Throws<KeyNotFoundException>(() => MotorPlantillas.Renderizar("{{noExiste}}", new Dictionary<string, object?>(), true));
        Assert.Throws<InvalidOperationException>(() =>
            MotorPlantillas.Renderizar("{{{x}}}", new Dictionary<string, object?> { ["x"] = "<b>" }, true));
        Assert.Equal("a-b-", MotorPlantillas.Renderizar("{{#l}}{{.}}-{{/l}}{{^v}}{{/v}}",
            new Dictionary<string, object?> { ["l"] = new[] { "a", "b" }, ["v"] = "" }, true));
    }

    [Fact]
    public void Mime_multipart_alternative_utf8_y_cabeceras_de_entregabilidad()
    {
        var contenido = Plantillas().Renderizar(EjemplosCorreo.Todos()[0]);
        using var mime = CorreoService.ConstruirMensaje(Smtp,
            MensajeCorreo.Desde(contenido with { Asunto = "Asunto\r\nBcc: victima@x.test" }, "est@cecar.test", "María José Núñez"));

        var from = Assert.Single(mime.From.Mailboxes);
        Assert.Equal("CDITutorPro – CECAR", from.Name);
        Assert.Equal("notificaciones@cecar.test", from.Address);
        Assert.Equal("coordinacion@cecar.test", Assert.Single(mime.ReplyTo.Mailboxes).Address);
        Assert.Equal("María José Núñez", Assert.Single(mime.To.Mailboxes).Name);
        Assert.EndsWith("@cecar.test", mime.MessageId);
        Assert.NotEqual(default, mime.Date);
        Assert.Equal("auto-generated", mime.Headers["Auto-Submitted"]);
        Assert.Equal("Asunto  Bcc: victima@x.test", mime.Subject); // sin salto de línea: no hay inyección de cabeceras
        Assert.Empty(mime.Bcc);

        var alternativa = Assert.IsType<MultipartAlternative>(mime.Body);
        var partes = alternativa.OfType<TextPart>().ToList();
        Assert.Equal(new[] { "text/plain", "text/html" }, partes.Select(p => p.ContentType.MimeType).ToArray());
        Assert.All(partes, p => Assert.Equal("utf-8", p.ContentType.Charset));

        using var ms = new MemoryStream();
        mime.WriteTo(ms);
        var crudo = System.Text.Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("=?utf-8?", crudo); // nombre visible con "–" y tildes codificados (RFC 2047)
        Assert.Contains("Message-Id: <", crudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sin_configuracion_falla_antes_de_conectar()
    {
        Assert.Throws<InvalidOperationException>(() => CorreoService.ValidarConfiguracion(new SmtpSettings()));
        CorreoService.ValidarConfiguracion(Smtp);
    }

    [Fact]
    public void Html_a_texto_conserva_enlaces_y_quita_estilos()
    {
        var t = HtmlATexto.Convertir("<html><head><style>p{color:red}</style></head><body><p>Hola &amp; adiós</p><a href=\"https://x.test/a\">Responder</a></body></html>");
        Assert.Contains("Hola & adiós", t);
        Assert.Contains("Responder (https://x.test/a)", t);
        Assert.DoesNotContain("color", t);
    }
}

/// <summary>Los flujos reales (crear tutoría) envían HTML + texto con la arquitectura de plantillas.</summary>
public class CorreosFlujoTests : IClassFixture<Agendamiento.Tests.Infrastructure.ApiFactory>
{
    private readonly Agendamiento.Tests.Infrastructure.ApiFactory _f;
    public CorreosFlujoTests(Agendamiento.Tests.Infrastructure.ApiFactory f) => _f = f;

    [Fact]
    public async Task Crear_tutoria_envia_correo_multiparte_con_datos_escapados()
    {
        var estId = Agendamiento.Tests.Infrastructure.Datos.CrearUsuario(_f, "flujo.est@test.com", Agendamiento.Tests.Infrastructure.Seed.RolEstudiante);
        var horarioId = Agendamiento.Tests.Infrastructure.Datos.CrearHorario(_f, Agendamiento.Tests.Infrastructure.Seed.Docente);
        var tutorId = _f.ConDb(db => Agendamiento.Tests.Infrastructure.Seed.IdDe(db, Agendamiento.Tests.Infrastructure.Seed.Tutor));
        using var admin = await _f.ClienteComoAsync(Agendamiento.Tests.Infrastructure.Seed.Admin);
        var resp = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(admin, "/api/Tutoria", new
        {
            idioma = "Inglés", nivel = "B1", tema = "<i>Viajes</i>", modalidad = "Virtual", estado = "Pendiente",
            fechaTutoria = DateTime.Today.AddHours(8), usuarioId = tutorId, horarioId, estudianteIds = new[] { estId }
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, resp.StatusCode);

        var correo = _f.Correo.Enviados.Last(c => c.Destinatario == "flujo.est@test.com");
        Assert.Equal("Nueva Tutoría Asignada - Inglés B1", correo.Asunto);
        Assert.Contains("&lt;i&gt;Viajes&lt;/i&gt;", correo.Html);
        Assert.Contains("Tema: <i>Viajes</i>", correo.Texto);
        Assert.True(_f.ConDb(db => db.Notificaciones.Any(n => n.Usuario_idUsuario == estId && n.asunto == correo.Asunto)));
    }

    [Fact]
    public async Task Previsualizacion_y_envio_de_prueba_no_existen_fuera_de_Development()
    {
        using var anonimo = _f.CreateClient();
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await anonimo.GetAsync("/dev/correos")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await anonimo.GetAsync("/dev/correos/nueva-tutoria")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized,
            (await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(anonimo, "/dev/correos/nueva-tutoria/enviar", new { para = "x@y.test" })).StatusCode);
        using var admin = await _f.ClienteComoAsync(Agendamiento.Tests.Infrastructure.Seed.Admin);
        Assert.Equal(System.Net.HttpStatusCode.NotFound,
            (await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(admin, "/dev/correos/nueva-tutoria/enviar", new { para = "x@y.test" })).StatusCode);
        Assert.Empty(_f.Correo.Enviados.Where(c => c.Destinatario == "x@y.test"));
    }
}