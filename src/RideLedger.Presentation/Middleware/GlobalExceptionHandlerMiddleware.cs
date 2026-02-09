using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace RideLedger.Presentation.Middleware;

/// <summary>
/// Global exception handling middleware following RFC 9457 Problem Details
/// Converts unhandled exceptions to standardized problem details responses
/// Senior developer note: Middleware executes in order - register early in pipeline
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> _logger)
    {
        _next = next;
        this._logger = _logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
            traceId,
            context.Request.Path,
            context.Request.Method);

        var (statusCode, title, detail) = MapExceptionToResponse(exception);

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc9457",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
            traceId,
            timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }

    private static (int StatusCode, string Title, string Detail) MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException nullEx => (
                (int)HttpStatusCode.BadRequest,
                "Bad Request",
                $"Required parameter '{nullEx.ParamName}' was null"),

            ArgumentException argEx => (
                (int)HttpStatusCode.BadRequest,
                "Bad Request",
                argEx.Message),

            InvalidOperationException invalidOp => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Operation",
                invalidOp.Message),

            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource"),

            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "Not Found",
                "The requested resource was not found"),

            TimeoutException => (
                (int)HttpStatusCode.RequestTimeout,
                "Request Timeout",
                "The request took too long to process"),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please contact support if the problem persists.")
        };
    }
}
