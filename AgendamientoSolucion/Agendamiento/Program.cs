using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Services;
using AgendamientoGestion.Logica.Models;
using AgendamientoGestion.Persistencia;
using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// 🔹 CONFIGURACIÓN DE CORS
// ----------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()    // Permite solicitudes desde cualquier origen (útil para pruebas)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// ----------------------
// 🔹 REPOSITORIOS
// ----------------------
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<ITutoriaRepository, TutoriaRepository>();
builder.Services.AddScoped<IRetroalimentacionRepository, RetroalimentacionRepository>();
builder.Services.AddScoped<INotificacionRepository, NotificacionRepository>();
builder.Services.AddScoped<IInformeRepository, InformeRepository>();
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<ITutoriaEstudianteRepository, TutoriaEstudianteRepository>();
builder.Services.AddScoped<IEncuestaRepository, EncuestaRepository>();

// ----------------------
// 🔹 SERVICIOS
// ----------------------
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ITutoriaService, TutoriaService>();
builder.Services.AddScoped<IRetroalimentacionService, RetroalimentacionService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IInformeService, InformeService>();
builder.Services.AddScoped<IHorarioServices, HorarioServices>();
builder.Services.AddScoped<IMetricasService, MetricasService>();
builder.Services.AddScoped<IEncuestaService, EncuestaService>();

// 🔹 Servicios de correo
builder.Services.AddScoped<ICorreoService, CorreoService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

// 🔹 Configuración SMTP
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// 🔹 Servicio en segundo plano para recordatorios automáticos
builder.Services.AddHostedService<Agendamiento.Services.RecordatorioTutoriaBackgroundService>();

// 🔹 Conexión BD
var conexion = builder.Configuration.GetConnectionString("local");
builder.Services.AddDbContext<AgendamientoDbContext>(option => option.UseSqlServer(conexion));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ----------------------
// 🔹 PIPELINE HTTP
// ----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 🔹 IMPORTANTE: activar CORS ANTES de los controladores
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
