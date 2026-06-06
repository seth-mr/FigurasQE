using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MicroservicioFiguras.Helpers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception while processing request.");

        var (statusCode, title, detail) = MapException(exception);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            })
            .ExecuteAsync(httpContext);

        return true;
    }

    private (int StatusCode, string Title, string? Detail) MapException(Exception exception)
    {
        if (HasPostgresConnectivityFailure(exception))
        {
            return (
                StatusCodes.Status503ServiceUnavailable,
                "Database unavailable.",
                _environment.IsDevelopment() ? exception.Message : "The database is currently unavailable.");
        }

        return exception switch
        {
            ValidationException validationException =>
                (StatusCodes.Status400BadRequest, "Validation error.", validationException.Message),

            KeyNotFoundException keyNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found.", keyNotFoundException.Message),

            UnauthorizedAccessException unauthorizedAccessException =>
                (StatusCodes.Status403Forbidden, "Forbidden.", unauthorizedAccessException.Message),

            InvalidOperationException invalidOperationException =>
                (StatusCodes.Status400BadRequest, "Invalid operation.", invalidOperationException.Message),

            DbUpdateException dbUpdateException =>
                (
                    StatusCodes.Status409Conflict,
                    "Database operation failed.",
                    _environment.IsDevelopment() ? dbUpdateException.Message : "The request could not be completed because of a database conflict."),

            _ =>
                (
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.",
                    _environment.IsDevelopment() ? exception.Message : null)
        };
    }

    private static bool HasPostgresConnectivityFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException)
            {
                return true;
            }
        }

        return false;
    }
}