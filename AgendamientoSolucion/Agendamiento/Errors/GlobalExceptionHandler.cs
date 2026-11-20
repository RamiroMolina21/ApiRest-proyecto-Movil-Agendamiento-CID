using AgendamientoGestion.Logica.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Errors
{
    /// <summary>
    /// Traduce las excepciones de la capa de lógica a respuestas RFC 9457 (application/problem+json).
    /// Fuera de Development nunca expone mensajes internos de excepciones no controladas.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        public const string MensajeErrorInterno = "Error interno del servidor";

        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            IProblemDetailsService problemDetailsService,
            IHostEnvironment environment,
            ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _environment = environment;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, title) = exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
                BusinessException => (StatusCodes.Status400BadRequest, "Regla de negocio incumplida"),
                UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autenticado"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
                NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
                DemasiadasSolicitudesException => (StatusCodes.Status429TooManyRequests, "Demasiadas solicitudes"),
                BadHttpRequestException bad => (bad.StatusCode, "Solicitud inválida"),
                _ => (StatusCodes.Status500InternalServerError, MensajeErrorInterno)
            };

            var problem = new ProblemDetails { Status = status, Title = title };

            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Excepción no controlada en {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);

                problem.Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "Ocurrió un error inesperado. Si persiste, contacte al administrador con el traceId.";
                if (_environment.IsDevelopment())
                {
                    problem.Extensions["exceptionType"] = exception.GetType().FullName;
                }
                // Compatibilidad: los clientes muestran "message".
                problem.Extensions["message"] = MensajeErrorInterno;
            }
            else
            {
                _logger.LogInformation("Solicitud rechazada con {Status}: {Mensaje}", status, exception.Message);
                problem.Detail = exception is BadHttpRequestException ? "La solicitud no es válida." : exception.Message;
            }

            if (exception is DemasiadasSolicitudesException limite)
            {
                httpContext.Response.Headers.RetryAfter = limite.ReintentarEnSegundos.ToString();
            }

            httpContext.Response.StatusCode = status;
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }
    }
}
