using Microsoft.AspNetCore.Http;
using RideLedger.Application.Common;
using System.Security.Claims;

namespace RideLedger.Infrastructure.Authentication;

/// <summary>
/// Tenant provider implementation extracting tenant context from JWT claims
/// Senior developer pattern: Encapsulate authentication context access
/// </summary>
public sealed class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available");

        // First check HttpContext items (set by TenantAuthorizationFilter)
        if (httpContext.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
        {
            return tenantId;
        }

        // Fallback to claims
        var tenantIdClaim = httpContext.User.FindFirst("tenant_id")?.Value
            ?? throw new UnauthorizedAccessException("Tenant ID claim is missing from JWT token");

        if (!Guid.TryParse(tenantIdClaim, out var parsedTenantId))
        {
            throw new UnauthorizedAccessException("Tenant ID claim has invalid format");
        }

        return parsedTenantId;
    }

    public string GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available");

        return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID claim is missing from JWT token");
    }

    public string? GetUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        return httpContext?.User.FindFirst(ClaimTypes.Email)?.Value
            ?? httpContext?.User.FindFirst("email")?.Value;
    }
}
