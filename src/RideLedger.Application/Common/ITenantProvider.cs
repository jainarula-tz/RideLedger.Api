namespace RideLedger.Application.Common;

/// <summary>
/// Interface for accessing current tenant context
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant ID from the authentication context
    /// </summary>
    Guid GetTenantId();

    /// <summary>
    /// Gets the current user identifier
    /// </summary>
    string GetUserId();

    /// <summary>
    /// Gets the current user's email if available
    /// </summary>
    string? GetUserEmail();
}
