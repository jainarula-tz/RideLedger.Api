using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace RideLedger.Presentation.Middleware;

/// <summary>
/// Request/Response logging middleware with correlation tracking
/// Logs HTTP requests and responses with timing information
/// Senior developer pattern: Use structured logging for observability
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> _logger)
    {
        _next = next;
        this._logger = _logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();

        // Add correlation ID to response headers for client tracking
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        // Log request
        _logger.LogInformation(
            "HTTP {Method} {Path} started. CorrelationId: {CorrelationId}, UserAgent: {UserAgent}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            context.Request.Headers.UserAgent.ToString());

        try
        {
            await _next(context);

            stopwatch.Stop();

            // Log successful response
            _logger.LogInformation(
                "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failed response
            _logger.LogError(
                ex,
                "HTTP {Method} {Path} failed after {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                correlationId);

            throw; // Re-throw for GlobalExceptionHandlerMiddleware
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in request header
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}
