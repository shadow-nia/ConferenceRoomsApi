using ConferenceRooms.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConferenceRooms.Api.Middleware;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Request conflict", exception.Message),
            BusinessValidationException =>
                (StatusCodes.Status422UnprocessableEntity, "Business rule violation", exception.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Request conflict", "A resource with the same unique value already exists."),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error", "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        });
    }
}
