namespace RideLedger.Domain.Primitives;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the UTC timestamp when the event occurred
    /// </summary>
    DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Gets the tenant identifier
    /// </summary>
    Guid TenantId { get; }
}

/// <summary>
/// Base record for domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    
    public Guid TenantId { get; init; }

    protected DomainEvent(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
