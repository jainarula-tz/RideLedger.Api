using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace RideLedger.Presentation.Filters;

/// <summary>
/// Performance monitoring filter with telemetry
/// Tracks action execution time and logs slow requests
/// Senior developer pattern: Performance observability at action level
/// </summary>
public sealed class PerformanceMonitoringFilter : IAsyncActionFilter
{
    private readonly ILogger<PerformanceMonitoringFilter> _logger;
    private const int SlowRequestThresholdMs = 1000;

    public PerformanceMonitoringFilter(ILogger<PerformanceMonitoringFilter> _logger)
    {
        this._logger = _logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var actionName = context.ActionDescriptor.DisplayName;

        _logger.LogDebug("Action {ActionName} started", actionName);

        var result = await next();

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "SLOW REQUEST: Action {ActionName} completed in {ElapsedMs}ms (threshold: {ThresholdMs}ms). " +
                "Path: {Path}, StatusCode: {StatusCode}",
                actionName,
                elapsedMs,
                SlowRequestThresholdMs,
                context.HttpContext.Request.Path,
                context.HttpContext.Response.StatusCode);
        }
        else
        {
            _logger.LogDebug(
                "Action {ActionName} completed in {ElapsedMs}ms",
                actionName,
                elapsedMs);
        }

        // Add performance header for client-side monitoring
        context.HttpContext.Response.Headers.Append("X-Response-Time-Ms", elapsedMs.ToString());
    }
}
