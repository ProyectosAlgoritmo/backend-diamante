using System.Net;
using System.Text.Json;

namespace BackendDiamante.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ArgumentNullException or ArgumentException
                => (HttpStatusCode.BadRequest, "La solicitud contiene datos inválidos."),

            UnauthorizedAccessException
                => (HttpStatusCode.Unauthorized, exception.Message),

            KeyNotFoundException
                => (HttpStatusCode.NotFound, "El recurso solicitado no existe."),

            InvalidOperationException
                => (HttpStatusCode.BadRequest, exception.Message),

            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error interno. Intenta de nuevo más tarde.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            success = false,
            message,
            // Solo exponer detalle en desarrollo
            detail  = IsProduction() ? null : exception.Message
        });

        return context.Response.WriteAsync(body);
    }

    // Evitar inyectar IHostEnvironment en el middleware para mantenerlo simple
    private static bool IsProduction() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
}
