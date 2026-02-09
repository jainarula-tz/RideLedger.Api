using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace RideLedger.Presentation.Filters;

/// <summary>
/// Action filter for FluentValidation integration
/// Validates request models before action execution
/// Senior developer pattern: Fail fast with validation at API boundary
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<ValidationFilter> _logger;

    public ValidationFilter(ILogger<ValidationFilter> _logger)
    {
        this._logger = _logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Check ModelState for DataAnnotations validation errors
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new
                {
                    Field = x.Key,
                    Error = e.ErrorMessage
                }))
                .ToList();

            _logger.LogWarning(
                "Validation failed for {ActionName}. Errors: {@Errors}",
                context.ActionDescriptor.DisplayName,
                errors);

            context.Result = new BadRequestObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc9457",
                title = "Validation Failed",
                status = 400,
                errors = errors.ToDictionary(e => e.Field, e => new[] { e.Error }),
                instance = context.HttpContext.Request.Path.Value
            });

            return;
        }

        await next();
    }
}
