using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace RideLedger.Presentation.Filters;

/// <summary>
/// Authorization filter for JWT token validation and tenant extraction
/// Validates tenant_id claim and enforces multi-tenant isolation
/// Senior developer pattern: Use filters for cross-cutting authorization concerns
/// </summary>
public sealed class TenantAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<TenantAuthorizationFilter> _logger;

    public TenantAuthorizationFilter(ILogger<TenantAuthorizationFilter> _logger)
    {
        this._logger = _logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Allow anonymous endpoints
        var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em is IAllowAnonymous);

        if (hasAllowAnonymous)
        {
            return;
        }

        var user = context.HttpContext.User;

        // Check if user is authenticated
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning(
                "Unauthorized access attempt to {Path}",
                context.HttpContext.Request.Path);

            context.Result = new UnauthorizedObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc9457",
                title = "Unauthorized",
                status = 401,
                detail = "Valid JWT token is required",
                instance = context.HttpContext.Request.Path.Value
            });

            return;
        }

        // Extract and validate tenant_id claim
        var tenantIdClaim = user.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning(
                "Missing or invalid tenant_id claim for user {UserId}",
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");

            context.Result = new ForbidResult();
            return;
        }

        // Add tenant ID to HttpContext items for downstream use
        context.HttpContext.Items["TenantId"] = tenantId;

        _logger.LogDebug(
            "Tenant authorization successful. TenantId: {TenantId}, UserId: {UserId}",
            tenantId,
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        await Task.CompletedTask;
    }
}
