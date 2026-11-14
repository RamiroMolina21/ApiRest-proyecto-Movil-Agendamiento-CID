using Agendamiento.Tests.Infrastructure;
using AgendamientoGestion.Entidades.Entities;
using AgendamientoGestion.Persistencia.DbContexts;
using System.Net;
using System.Net.Http.Json;

namespace Agendamiento.Tests;

/// <summary>
/// DELETE /api/Usuario/{id}: eliminación definitiva por rol. Datos propios se borran; compartidos se conservan
/// desvinculados (FK a null); nunca se tocan tutorías ni datos de otros usuarios; sin huérfanos.
/// </summary>
public class EliminacionUsuariosTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public EliminacionUsuariosTests(ApiFactory factory)
    {
        _f = factory;
    }

    private int Usuario(string correo, int rol) => Datos.CrearUsuario(_f, correo, rol);

    private int Horario(int duenoId, string estado = "Disponible") => _f.ConDb(db =>
    {
        var h = new Horario
        {
            titulo = "H elim", fechaInicio = DateTime.Today, fechaFin = DateTime.Today.AddDays(30),
            horaInicio = DateTime.Today.AddHours(8), horaFin = DateTime.Today.AddHours(9),
            cupos = "10", espacio = "Sala E", estado = estado, Usuario_idUsuario = duenoId
        };
        db.Horarios.Add(h);
        db.SaveChanges();
        return h.idHorario;
    });

    private int Tutoria(int horarioId, int asignadoId, string estado, DateTime fecha, params int[] estudiantes) => _f.ConDb(db =>
    {
        var t = new Tutoria
        {
            idioma = "Inglés", nivel = "B1", tema = "Elim", modalidad = "Virtual", estado = estado,
            fechaHora = fecha, Usuario_idUsuario = asignadoId, Horario_idHorario = horarioId
        };
        db.Tutorias.Add(t);
        db.SaveChanges();
        foreach (var e in estudiantes)
            db.TutoriaEstudiantes.Add(new TutoriaEstudiante { Tutoria_idTutoria = t.idTutoria, Usuario_idUsuario = e });
        db.SaveChanges();
        return t.idTutoria;
    });

    private void Notificaciones(int usuarioId, int n) => _f.ConDb(db =>
    {
        for (var i = 0; i < n; i++)
            db.Notificaciones.Add(new Notificacion { fecha = DateTime.Now, asunto = "x", descripcion = "y", Usuario_idUsuario = usuarioId });
        return db.SaveChanges();
    });

    /// <summary>Ninguna FK a Usuario apunta a un usuario inexistente (además de la FK de la BD).</summary>
    private static void SinHuerfanos(AgendamientoDbContext db)
    {
        var ids = db.Usuarios.Select(u => u.idUsuario).ToHashSet();
        Assert.All(db.Tutorias.Where(t => t.Usuario_idUsuario != null).ToList(), t => Assert.Contains(t.Usuario_idUsuario!.Value, ids));
        Assert.All(db.Horarios.Where(h => h.Usuario_idUsuario != null).ToList(), h => Assert.Contains(h.Usuario_idUsuario!.Value, ids));
        Assert.All(db.Retroalimentaciones.Where(r => r.Usuario_idUsuario != null).ToList(), r => Assert.Contains(r.Usuario_idUsuario!.Value, ids));
        Assert.All(db.Informes.Where(i => i.Usuario_idUsuario != null).ToList(), i => Assert.Contains(i.Usuario_idUsuario!.Value, ids));
        Assert.All(db.AsuntosAtendidos.Where(a => a.Usuario_idUsuario != null).ToList(), a => Assert.Contains(a.Usuario_idUsuario!.Value, ids));
        Assert.All(db.TutoriaEstudiantes.ToList(), te => Assert.Contains(te.Usuario_idUsuario, ids));
        Assert.All(db.Notificaciones.ToList(), n => Assert.Contains(n.Usuario_idUsuario, ids));
        var tutorias = db.Tutorias.Select(t => t.idTutoria).ToHashSet();
        Assert.All(db.TutoriaEstudiantes.ToList(), te => Assert.Contains(te.Tutoria_idTutoria, tutorias));
        Assert.All(db.Retroalimentaciones.ToList(), r => Assert.Contains(r.Tutoria_idTutoria, tutorias));
    }

    [Fact]
    public async Task Docente_con_tutorias_finalizadas_canceladas_y_pendientes_se_elimina_y_se_conserva_lo_compartido()
    {
        var docente = Usuario("elim.docente@test.com", Seed.RolDocente);
        var otroEst = Usuario("elim.otro.est@test.com", Seed.RolEstudiante);
        var est = Usuario("elim.est.a@test.com", Seed.RolEstudiante);
        var horarioConTutorias = Horario(docente, "Ocupado");
        var horarioLibre = Horario(docente);
        var finalizada = Tutoria(horarioConTutorias, docente, "Finalizada", DateTime.Today.AddDays(-3), est, otroEst);
        var cancelada = Tutoria(horarioConTutorias, docente, "Cancelada", DateTime.Today.AddDays(-1), otroEst);
        var pendiente = Tutoria(horarioConTutorias, docente, "Pendiente", DateTime.Today.AddDays(5), est, otroEst);
        _f.ConDb(db =>
        {
            db.Retroalimentaciones.Add(new Retroalimentacion { comentario = "bien", calificacion = 5, fecha = DateTime.Now, Tutoria_idTutoria = finalizada, Usuario_idUsuario = docente });
            db.Informes.Add(new Informe { descripcion = "inf", fechaGeneracion = DateTime.Now, Tutoria_idTutoria = finalizada, Usuario_idUsuario = docente });
            return db.SaveChanges();
        });
        Notificaciones(docente, 3);
        Notificaciones(otroEst, 2);
        var inscripcionesOtro = _f.ConDb(db => db.TutoriaEstudiantes.Count(te => te.Usuario_idUsuario == otroEst));

        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var resp = await admin.DeleteAsync($"/api/Usuario/{docente}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.JsonAsync();
        Assert.Equal("Usuario eliminado exitosamente", json.Str("message"));
        var resumen = json["resumen"]!;
        Assert.Equal(3, resumen["tutoriasDesasignadas"]!.GetValue<int>());
        Assert.Equal(1, resumen["horariosEliminados"]!.GetValue<int>());
        Assert.Equal(1, resumen["horariosConservados"]!.GetValue<int>());
        Assert.Equal(3, resumen["notificacionesEliminadas"]!.GetValue<int>());
        Assert.Equal(1, resumen["retroalimentacionesConservadas"]!.GetValue<int>());
        Assert.Equal(1, resumen["informesConservados"]!.GetValue<int>());

        _f.ConDb(db =>
        {
            Assert.False(db.Usuarios.Any(u => u.idUsuario == docente));
            Assert.False(db.Horarios.Any(h => h.idHorario == horarioLibre));
            Assert.Null(db.Horarios.Single(h => h.idHorario == horarioConTutorias).Usuario_idUsuario);
            foreach (var t in new[] { finalizada, cancelada, pendiente })
            {
                var tut = db.Tutorias.Single(x => x.idTutoria == t);
                Assert.Null(tut.Usuario_idUsuario);
            }
            Assert.Equal("Pendiente", db.Tutorias.Single(x => x.idTutoria == pendiente).estado);
            Assert.Null(db.Retroalimentaciones.Single(r => r.Tutoria_idTutoria == finalizada).Usuario_idUsuario);
            Assert.Null(db.Informes.Single(i => i.Tutoria_idTutoria == finalizada).Usuario_idUsuario);
            // Los estudiantes y sus datos siguen intactos.
            Assert.Equal(inscripcionesOtro, db.TutoriaEstudiantes.Count(te => te.Usuario_idUsuario == otroEst));
            Assert.Equal(2, db.TutoriaEstudiantes.Count(te => te.Usuario_idUsuario == est));
            Assert.Equal(2, db.Notificaciones.Count(n => n.Usuario_idUsuario == otroEst));
            SinHuerfanos(db);
            return 0;
        });

        // La API sigue respondiendo con los registros desvinculados.
        var tutoria = await (await admin.GetAsync($"/api/Tutoria/{pendiente}")).JsonAsync();
        Assert.Null(tutoria["usuarioId"]);
        Assert.Null(tutoria["usuarioNombre"]);
        Assert.Null(tutoria["horarioUsuarioId"]);
        Assert.Equal(2, tutoria["estudiantes"]!.AsArray().Count);
        var horario = await (await admin.GetAsync($"/api/Horario/{horarioConTutorias}")).JsonAsync();
        Assert.Null(horario["usuarioId"]);
        Assert.Null(horario["usuarioNombre"]);
        var retros = (await (await admin.GetAsync($"/api/Retroalimentacion/tutoria/{finalizada}")).JsonAsync()).AsArray();
        Assert.Null(retros.Single()!["usuarioId"]);
        Assert.Null(retros.Single()!["usuarioNombre"]);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/Tutoria")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/Horario")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/Metricas/tutorias")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/Notificacion/reporte-excel/{finalizada}")).StatusCode);

        // La tutoría pendiente sin asignar se puede reasignar.
        var tutorNuevo = Usuario("elim.tutor.nuevo@test.com", Seed.RolTutor);
        var put = await admin.PutAsJsonAsync($"/api/Tutoria/{pendiente}", new
        {
            idioma = "Inglés", nivel = "B1", tema = "Elim", modalidad = "Virtual", estado = "Pendiente",
            fechaTutoria = DateTime.Today.AddDays(5), usuarioId = tutorNuevo, horarioId = horarioConTutorias
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(tutorNuevo, (await put.JsonAsync())["usuarioId"]!.GetValue<int>());
    }

    [Fact]
    public async Task Estudiante_en_tutorias_compartidas_solo_pierde_sus_datos()
    {
        var docente = Usuario("elim2.docente@test.com", Seed.RolDocente);
        var est = Usuario("elim2.est@test.com", Seed.RolEstudiante);
        var companero = Usuario("elim2.companero@test.com", Seed.RolEstudiante);
        var h = Horario(docente, "Ocupado");
        var finalizada = Tutoria(h, docente, "Finalizada", DateTime.Today.AddDays(-2), est, companero);
        var pendiente = Tutoria(h, docente, "Pendiente", DateTime.Today.AddDays(2), est, companero);
        Notificaciones(est, 2);
        _f.ConDb(db =>
        {
            db.EncuestasSatisfaccion.AddRange(
                new EncuestaSatisfaccion { Tutoria_idTutoria = finalizada, tokenRespuesta = Guid.NewGuid().ToString("N"), estudianteCorreo = "elim2.est@test.com", estudianteNombre = "Est", tokenUsado = true, calificacion = 4 },
                new EncuestaSatisfaccion { Tutoria_idTutoria = pendiente, tokenRespuesta = Guid.NewGuid().ToString("N"), estudianteCorreo = "elim2.est@test.com", estudianteNombre = "Est" },
                new EncuestaSatisfaccion { Tutoria_idTutoria = finalizada, tokenRespuesta = Guid.NewGuid().ToString("N"), estudianteCorreo = "elim2.companero@test.com", estudianteNombre = "Comp" });
            return db.SaveChanges();
        });

        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);
        var resp = await aux.DeleteAsync($"/api/Usuario/{est}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var resumen = (await resp.JsonAsync())["resumen"]!;
        Assert.Equal(2, resumen["inscripcionesEliminadas"]!.GetValue<int>());
        Assert.Equal(2, resumen["notificacionesEliminadas"]!.GetValue<int>());
        Assert.Equal(1, resumen["encuestasEliminadas"]!.GetValue<int>());
        Assert.Equal(1, resumen["encuestasAnonimizadas"]!.GetValue<int>());
        Assert.Equal(0, resumen["tutoriasDesasignadas"]!.GetValue<int>());

        _f.ConDb(db =>
        {
            Assert.True(db.Tutorias.Any(t => t.idTutoria == finalizada));
            Assert.True(db.Tutorias.Any(t => t.idTutoria == pendiente));
            Assert.Equal(docente, db.Tutorias.Single(t => t.idTutoria == pendiente).Usuario_idUsuario);
            Assert.Equal(2, db.TutoriaEstudiantes.Count(te => te.Usuario_idUsuario == companero));
            Assert.False(db.TutoriaEstudiantes.Any(te => te.Usuario_idUsuario == est));
            var respondida = db.EncuestasSatisfaccion.Single(e => e.Tutoria_idTutoria == finalizada && e.calificacion == 4);
            Assert.Equal($"eliminado-{respondida.idEncuesta}@{AgendamientoGestion.Persistencia.UsuarioRepository.DominioAnonimizado}", respondida.estudianteCorreo);
            Assert.Equal(AgendamientoGestion.Persistencia.UsuarioRepository.NombreAnonimizado, respondida.estudianteNombre);
            Assert.Equal(4, respondida.calificacion);
            Assert.True(db.EncuestasSatisfaccion.Any(e => e.estudianteCorreo == "elim2.companero@test.com"));
            Assert.False(db.EncuestasSatisfaccion.Any(e => e.estudianteCorreo == "elim2.est@test.com"));
            SinHuerfanos(db);
            return 0;
        });
    }

    [Fact]
    public async Task Tutor_y_administrador_conservan_tutorias_y_atendidos_sin_autor()
    {
        var tutor = Usuario("elim3.tutor@test.com", Seed.RolTutor);
        var h = Horario(Datos.CrearUsuario(_f, "elim3.docente@test.com", Seed.RolDocente), "Ocupado");
        var pendiente = Tutoria(h, tutor, "Pendiente", DateTime.Today.AddDays(3));

        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/Usuario/{tutor}")).StatusCode);
        Assert.Null(_f.ConDb(db => db.Tutorias.Single(t => t.idTutoria == pendiente).Usuario_idUsuario));
        Assert.NotNull(_f.ConDb(db => db.Horarios.Single(x => x.idHorario == h).Usuario_idUsuario)); // el horario es de otro

        const string admin2 = "elim3.admin2@test.com";
        var admin2Id = Usuario(admin2, Seed.RolAdministrador);
        using var cAdmin2 = await _f.ClienteComoAsync(admin2);
        Assert.Equal(HttpStatusCode.OK, (await cAdmin2.PostAsJsonAsync("/api/Atencion/atendidos", new { tipo = "tutorias-vencidas", claves = new[] { pendiente } })).StatusCode);

        var del = await admin.DeleteAsync($"/api/Usuario/{admin2Id}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);
        Assert.Equal(1, (await del.JsonAsync())["resumen"]!["atendidosConservados"]!.GetValue<int>());
        var atendido = (await (await admin.GetAsync("/api/Atencion/atendidos?tipo=tutorias-vencidas")).JsonAsync()).AsArray()
            .Single(n => n!["clave"]!.GetValue<int>() == pendiente)!;
        Assert.Null(atendido["usuarioId"]);
        Assert.Null(atendido["usuarioNombre"]);

        // El token del eliminado deja de valer.
        Assert.Equal(HttpStatusCode.Unauthorized, (await cAdmin2.GetAsync("/api/Auth/me")).StatusCode);
        _f.ConDb(db => { SinHuerfanos(db); return 0; });
    }

    [Fact]
    public async Task Reglas_se_mantienen()
    {
        using var admin = await _f.ClienteComoAsync(Seed.Admin);
        var self = await admin.DeleteAsync($"/api/Usuario/{_f.ConDb(db => Seed.IdDe(db, Seed.Admin))}");
        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/Usuario/999999")).StatusCode);

        var docente = Usuario("elim4.docente@test.com", Seed.RolDocente);
        using var aux = await _f.ClienteComoAsync(Seed.Auxiliar);
        Assert.Equal(HttpStatusCode.Forbidden, (await aux.DeleteAsync($"/api/Usuario/{docente}")).StatusCode);
        using var doc = await _f.ClienteComoAsync(Seed.Docente);
        Assert.Equal(HttpStatusCode.Forbidden, (await doc.DeleteAsync($"/api/Usuario/{docente}")).StatusCode);
        Assert.True(_f.ConDb(db => db.Usuarios.Any(u => u.idUsuario == docente)));
    }

    [Fact]
    public async Task No_se_elimina_al_ultimo_administrador_activo()
    {
        // admin3 obtiene token y su estado "activa" queda en caché; luego se desactiva por fuera de la API (BD),
        // así que el único Administrador ACTIVO es el seed: borrarlo debe dar 409 aunque el token aún valga.
        const string admin3 = "elim5.admin3@test.com";
        var admin3Id = Usuario(admin3, Seed.RolAdministrador);
        using var c = await _f.ClienteComoAsync(admin3);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/Auth/me")).StatusCode);
        _f.ConDb(db => { db.Usuarios.Single(u => u.idUsuario == admin3Id).activo = false; return db.SaveChanges(); });

        var adminSeed = _f.ConDb(db => Seed.IdDe(db, Seed.Admin));
        var resp = await c.DeleteAsync($"/api/Usuario/{adminSeed}");
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        Assert.Contains("último Administrador", (await resp.JsonAsync()).Str("message"));
        Assert.True(_f.ConDb(db => db.Usuarios.Any(u => u.idUsuario == adminSeed)));

        _f.ConDb(db => { db.Usuarios.Single(u => u.idUsuario == admin3Id).activo = true; return db.SaveChanges(); });
    }
}
