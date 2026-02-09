using RideLedger.Domain.Enums;

namespace RideLedger.Infrastructure.Persistence.Entities;

/// <summary>
/// INFRASTRUCTURE LAYER - Persistence Entity
/// EF Core entity for Account aggregate persistence
/// Separate from domain model to avoid polluting domain with EF concerns
/// </summary>
public sealed class AccountEntity
{
    public Guid AccountId { get; set; }
    public required string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public AccountStatus Status { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<LedgerEntryEntity> LedgerEntries { get; set; } = new List<LedgerEntryEntity>();
}
