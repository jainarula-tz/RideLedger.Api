namespace RideLedger.Infrastructure.Persistence.Entities;

/// <summary>
/// INFRASTRUCTURE LAYER - Persistence Entity
/// EF Core entity for Outbox pattern message storage
/// Ensures reliable event publishing with transactional consistency
/// </summary>
public sealed class OutboxMessageEntity
{
    public Guid MessageId { get; set; }
    public Guid TenantId { get; set; }
    public required string EventType { get; set; } = string.Empty;
    public required string Payload { get; set; } = string.Empty; // JSON serialized event
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
