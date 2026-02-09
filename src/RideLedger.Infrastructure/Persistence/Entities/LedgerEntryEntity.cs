using RideLedger.Domain.Enums;

namespace RideLedger.Infrastructure.Persistence.Entities;

/// <summary>
/// INFRASTRUCTURE LAYER - Persistence Entity
/// EF Core entity for LedgerEntry persistence
/// Immutable once created (append-only ledger)
/// </summary>
public sealed class LedgerEntryEntity
{
    public Guid LedgerEntryId { get; set; }
    public Guid AccountId { get; set; }
    public Guid TenantId { get; set; }
    public LedgerAccountType LedgerAccount { get; set; }
    public decimal? DebitAmount { get; set; }
    public decimal? CreditAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public SourceType SourceType { get; set; }
    public required string SourceReferenceId { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string? Metadata { get; set; } // JSONB for flexible storage
    public DateTime CreatedAt { get; set; }
    public required string CreatedBy { get; set; } = string.Empty;

    // Navigation properties
    public AccountEntity Account { get; set; } = null!;
}
