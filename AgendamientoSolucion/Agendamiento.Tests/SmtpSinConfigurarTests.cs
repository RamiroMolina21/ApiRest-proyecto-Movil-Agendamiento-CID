using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace Agendamiento.Tests;

/// <summary>
/// Igual que <see cref="ApiFactory"/> pero con el <see cref="CorreoService"/> REAL y SmtpSettings vacíos,
/// que es la configuración decidida para desarrollo (no se envían correos): cada envío lanza una excepción
/// antes de abrir ninguna conexión. Host "smtp.invalid" por si acaso: nunca resuelve.
/// </summary>
public class ApiFactorySmtpSinConfigurar : ApiFactory
{
    protected override IDictionary<string, string?> Configuracion
    {
        get
        {
            var c = base.Configuracion;
            c["SmtpSettings:Remitente"] = "";
            c["SmtpSettings:User"] = "";
            c["SmtpSettings:Password"] = "";
            c["SmtpSettings:Host"] = "smtp.invalid";
            return c;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICorreoService>();
            services.AddScoped<ICorreoService, CorreoService>();
        });
    }
}

public class SmtpSinConfigurarTests : IClassFixture<ApiFactorySmtpSinConfigurar>
{
    private readonly ApiFactorySmtpSinConfigurar _f;

    public SmtpSinConfigurarTests(ApiFactorySmtpSinConfigurar factory) => _f = factory;

    [Fact]
    public void El_correo_real_sin_configurar_lanza_excepcion()
    {
        // Precondición del escenario: si esto no lanzara, el resto de pruebas no demostraría nada.
        using var scope = _f.Services.CreateScope();
        var correo = scope.ServiceProvider.GetRequiredService<ICorreoService>();
        Assert.IsType<CorreoService>(correo);
        Assert.ThrowsAny<Exception>(() => correo.EnviarCorreo("alguien@cecar.test", "Asunto", "<p>x</p>"));
    }

    [Fact]
    public async Task Crear_asignar_editar_y_finalizar_tutoria_no_fallan_sin_smtp()
    {
        // El Docente agenda en SU horario y asigna la tutoría al Tutor (flujo MAUI/web).
        var horarioId = Datos.CrearHorario(_f, Seed.Docente);
        var tutorId = _f.ConDb(db => Seed.IdDe(db, Seed.Tutor));
        var estudianteId = _f.ConDb(db => Seed.IdDe(db, Seed.Estudiante1));
        var dto = new
        {
            idioma = "Inglés", nivel = "B1", tema = "Sin SMTP", modalidad = "Presencial", estado = "Pendiente",
            fechaTutoria = DateTime.Today.AddHours(8), usuarioId = tutorId, horarioId, cupos = 5
        };

        using var docente = await _f.ClienteComoAsync(Seed.Docente);
        using var tutor = await _f.ClienteComoAsync(Seed.Tutor);
        using var admin = await _f.ClienteComoAsync(Seed.Admin);

        // Crear (notificación automática al tutor por correo → falla y se registra en el log).
        var crear = await docente.PostAsJsonAsync("/api/Tutoria", dto);
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var tutoriaId = (int)(await crear.JsonAsync())["idTutoria"]!;
        Assert.True(_f.ConDb(db => db.Tutorias.Any(t => t.idTutoria == tutoriaId)));

        // Asignar estudiante (correo de asignación falla) → la asignación persiste.
        var asignar = await docente.PostAsJsonAsync($"/api/Tutoria/{tutoriaId}/estudiantes", new { estudianteIds = new[] { estudianteId } });
        Assert.Equal(HttpStatusCode.OK, asignar.StatusCode);
        Assert.True(_f.ConDb(db => db.TutoriaEstudiantes.Any(te => te.Tutoria_idTutoria == tutoriaId && te.Usuario_idUsuario == estudianteId)));

        // Editar (correo de cambio falla).
        var editar = await admin.PutAsJsonAsync($"/api/Tutoria/{tutoriaId}", dto with { tema = "Sin SMTP editada" });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        // Finalizar (correos de encuesta fallan) → queda finalizada y la encuesta se crea igualmente.
        var finalizar = await tutor.PutAsync($"/api/Tutoria/{tutoriaId}/finalizar", null);
        Assert.Equal(HttpStatusCode.OK, finalizar.StatusCode);
        Assert.Equal("Finalizada", _f.ConDb(db => db.Tutorias.Single(t => t.idTutoria == tutoriaId).estado));
        Assert.Equal(1, _f.ConDb(db => db.EncuestasSatisfaccion.Count(e => e.Tutoria_idTutoria == tutoriaId)));
    }
}
