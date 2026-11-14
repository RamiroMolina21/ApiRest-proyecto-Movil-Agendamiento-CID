using Agendamiento.Errors;
using Agendamiento.Security;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Models;
using AgendamientoGestion.Logica.Services;
using AgendamientoGestion.Persistencia;
using AgendamientoGestion.Persistencia.DbContexts;
using AgendamientoGestion.Persistencia.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// 🔹 OPCIONES (validadas al arrancar)
// ----------------------
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateOnStart();
// Falla al arrancar con un mensaje claro si Jwt:Key falta o es corta (ver JwtOptions.Validar).
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

builder.Services.AddOptions<LoginRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(LoginRateLimitOptions.SectionName));

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<EncuestaSettings>(builder.Configuration.GetSection(EncuestaSettings.SectionName));
builder.Services.AddSingleton(TimeProvider.System);

// ----------------------
// 🔹 CORS (orígenes desde configuración "Cors:AllowedOrigins")
// ----------------------
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>().Configure<IConfiguration>((options, configuration) =>
{
    var origenes = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    options.AddPolicy(CorsConfig.PolicyName, policy =>
    {
        policy.WithOrigins(origenes)
              .AllowAnyMethod()
              .AllowAnyHeader()                     // Authorization, Content-Type, ngrok-skip-browser-warning
              .WithExposedHeaders("Content-Disposition", "Retry-After", "X-Total-Count");
    });
});

// ----------------------
// 🔹 AUTENTICACIÓN JWT + AUTORIZACIÓN POR ROLES
// ----------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false; // conservar "sub", "rol", "correo" tal cual
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwt.KeyBytes),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtClaims.Correo,
            RoleClaimType = JwtClaims.Rol
        };
        // Cuenta borrada o desactivada → el token deja de valer (como mucho 30 s de retraso entre instancias).
        options.Events = new JwtBearerEvents { OnTokenValidated = ValidacionCuentaJwt.OnTokenValidated };
    });
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEstadoCuentaCache, EstadoCuentaCache>();
builder.Services.AddAuthorization(Politicas.Registrar);
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// ----------------------
// 🔹 RATE LIMITING (login): ventana fija por IP de cliente
// ----------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }
        return ValueTask.CompletedTask; // el cuerpo ProblemDetails lo escribe UseStatusCodePages
    };
});
builder.Services.AddOptions<CambioContrasenaRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(CambioContrasenaRateLimitOptions.SectionName));
builder.Services.AddOptions<RateLimiterOptions>().Configure<IOptions<CambioContrasenaRateLimitOptions>>((options, limits) =>
{
    var cfg = limits.Value;
    // Partición por usuario (claim "sub"): requiere que UseRateLimiter vaya DESPUÉS de UseAuthentication.
    options.AddPolicy(CambioContrasenaRateLimitOptions.PolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst(JwtClaims.Sub)?.Value is { } sub
                ? $"usuario:{sub}"
                : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida"}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = cfg.PermitLimit,
                Window = TimeSpan.FromSeconds(cfg.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddOptions<RateLimiterOptions>().Configure<IOptions<LoginRateLimitOptions>>((options, limits) =>
{
    var cfg = limits.Value;
    options.AddPolicy(LoginRateLimitOptions.PolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = cfg.PermitLimit,
                Window = TimeSpan.FromSeconds(cfg.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// ngrok / proxy inverso local: tomar la IP real del cliente de X-Forwarded-For (solo se confía en loopback,
// que es el valor por defecto de KnownProxies/KnownNetworks).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

// ----------------------
// 🔹 ERRORES: ProblemDetails (RFC 9457) para todo
// ----------------------
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ProblemDetailsDefaults.Customize);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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
builder.Services.AddScoped<IAsuntoAtendidoRepository, AsuntoAtendidoRepository>();
// Transacciones que abarcan varios repositorios (comparten el DbContext scoped).
builder.Services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

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
builder.Services.AddScoped<IAtencionService, AtencionService>();

// 🔹 Servicios de correo
builder.Services.AddScoped<ICorreoService, CorreoService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddSingleton<AgendamientoGestion.Logica.Correo.IPlantillasCorreo, AgendamientoGestion.Logica.Correo.PlantillasCorreo>();
builder.Services.AddScoped<IExcelService, ExcelService>();

// 🔹 Servicio en segundo plano para recordatorios automáticos
builder.Services.AddHostedService<Agendamiento.Services.RecordatorioTutoriaBackgroundService>();

// 🔹 Conexión BD
var conexion = builder.Configuration.GetConnectionString("local");
builder.Services.AddDbContext<AgendamientoDbContext>(option => option.UseSqlServer(conexion));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Agendamiento API", Version = "v1" });

    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Pegue solo el token devuelto por POST /api/Auth/login (sin el prefijo 'Bearer').",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", esquema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
});

var app = builder.Build();

// ----------------------
// 🔹 AVISOS DE ENTREGABILIDAD DE CORREO (ver docs/correo/entregabilidad.md)
// ----------------------
{
    var urlEncuesta = app.Services.GetRequiredService<IOptions<EncuestaSettings>>().Value.UrlBase;
    if (!Uri.TryCreate(urlEncuesta, UriKind.Absolute, out var uriEncuesta) || uriEncuesta.IsLoopback || uriEncuesta.Scheme != Uri.UriSchemeHttps)
    {
        // Un enlace a localhost/http en un correo real no funciona para el destinatario y es señal de spam.
        app.Logger.Log(app.Environment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning,
            "Encuesta:UrlBase ({Url}) no es una URL pública https: los enlaces de los correos de encuesta no funcionarán fuera de este equipo.", urlEncuesta);
    }

    var smtp = app.Services.GetRequiredService<IOptions<SmtpSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(smtp.User) && !string.Equals(smtp.DireccionRemitente, smtp.User.Trim(), StringComparison.OrdinalIgnoreCase))
    {
        app.Logger.LogWarning("SmtpSettings:Remitente distinto de la cuenta autenticada: el proveedor puede reescribir el From o el correo fallar DMARC.");
    }
}

// ----------------------
// 🔹 PIPELINE HTTP
// ----------------------
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(); // cuerpos ProblemDetails para 401/403/404/405/429 sin contenido

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS antes de autenticación para que el preflight OPTIONS no exija token
app.UseCors(CorsConfig.PolicyName);

// Autenticación antes del rate limiter: la política de cambio de contraseña particiona por usuario ("sub").
// El login (anónimo) sigue particionando por IP; validar un JWT es barato, no abre vía de abuso.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necesario para WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program
{
}
