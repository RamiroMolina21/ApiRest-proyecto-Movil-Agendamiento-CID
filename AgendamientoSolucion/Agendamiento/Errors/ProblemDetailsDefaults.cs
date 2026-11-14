using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Agendamiento.Errors
{
    /// <summary>
    /// Se aplica a TODOS los ProblemDetails (excepciones, validación automática de [ApiController],
    /// 401/403 del middleware de autenticación, 404/405/429 vía UseStatusCodePages):
    ///  - "message": texto legible en español (compatibilidad con los clientes que leen data.message).
    ///  - "traceId": para correlacionar con los logs.
    /// </summary>
    public static class ProblemDetailsDefaults
    {
        public static void Customize(ProblemDetailsContext context)
        {
            var problem = context.ProblemDetails;

            problem.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);

            // 401 por cuenta borrada/desactivada (ValidacionCuentaJwt): mensaje concreto en lugar del genérico.
            if (problem.Status == StatusCodes.Status401Unauthorized
                && context.HttpContext.Items[Agendamiento.Security.ValidacionCuentaJwt.ItemMensaje] is string mensajeAuth)
            {
                problem.Extensions.TryAdd("message", mensajeAuth);
            }

            if (!problem.Extensions.ContainsKey("message"))
            {
                problem.Extensions["message"] = ConstruirMensaje(problem);
            }
        }

        private static string ConstruirMensaje(Microsoft.AspNetCore.Mvc.ProblemDetails problem)
        {
            if (problem is HttpValidationProblemDetails validation && validation.Errors.Count > 0)
            {
                return string.Join("; ", validation.Errors.SelectMany(e => e.Value));
            }

            if (!string.IsNullOrWhiteSpace(problem.Detail))
            {
                return problem.Detail;
            }

            return problem.Status switch
            {
                StatusCodes.Status400BadRequest => "Solicitud inválida",
                StatusCodes.Status401Unauthorized => "Se requiere autenticación: envíe un token Bearer válido (puede haber expirado)",
                StatusCodes.Status403Forbidden => "No tiene permisos para realizar esta operación",
                StatusCodes.Status404NotFound => "Recurso no encontrado",
                StatusCodes.Status405MethodNotAllowed => "Método HTTP no permitido para este recurso",
                StatusCodes.Status415UnsupportedMediaType => "Tipo de contenido no soportado; use application/json",
                StatusCodes.Status429TooManyRequests => "Demasiados intentos. Espere un momento e intente de nuevo",
                >= 500 => GlobalExceptionHandler.MensajeErrorInterno,
                _ => problem.Title ?? "Error"
            };
        }
    }
}
