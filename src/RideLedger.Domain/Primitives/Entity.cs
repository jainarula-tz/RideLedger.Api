namespace RideLedger.Domain.Primitives;

/// <summary>
/// Base class for all entities with strongly-typed ID
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity
    /// </summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>
    /// Gets the tenant identifier for multi-tenant isolation
    /// </summary>
    public Guid TenantId { get; protected init; }

    protected Entity()
    {
    }

    protected Entity(TId id, Guid tenantId)
    {
        Id = id;
        TenantId = tenantId;
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id.Equals(other.Id) && TenantId.Equals(other.TenantId);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, TenantId);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
