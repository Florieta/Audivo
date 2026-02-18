using System.Net;
using System.Text.Json;
using Audivo.Application.Exceptions;

namespace Audivo.API.Middleware;

/// <summary>
/// Global exception handler that maps application exceptions to consistent ProblemDetails responses.
/// </summary>
public sealed partial class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            Application.Exceptions.ValidationException validationEx =>
                (HttpStatusCode.BadRequest, "Validation Error", validationEx.Message, validationEx.Errors),

            AuthenticationException authEx =>
                (HttpStatusCode.Unauthorized, "Authentication Error", authEx.Message, (IDictionary<string, string[]>?)null),

            NotFoundException notFoundEx =>
                (HttpStatusCode.NotFound, "Not Found", notFoundEx.Message, null),

            UnauthorizedAccessException =>
                (HttpStatusCode.Forbidden, "Forbidden", "You do not have permission to perform this action.", null),

            _ => (HttpStatusCode.InternalServerError, "Server Error",
                  _environment.IsDevelopment() ? exception.ToString() : "An unexpected error occurred.",
                  (IDictionary<string, string[]>?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            LogUnhandledException(_logger, exception);
        }
        else
        {
            LogHandledException(_logger, title, exception);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var response = new
        {
            status = (int)statusCode,
            title,
            detail,
            errors,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);

        await context.Response.WriteAsync(json);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception occurred")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handled exception: {Title}")]
    private static partial void LogHandledException(ILogger logger, string title, Exception exception);
}
