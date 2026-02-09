using RideLedger.Domain.Enums;
using RideLedger.Domain.Primitives;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Entities;

/// <summary>
/// Represents an immutable ledger entry in the double-entry accounting system
/// </summary>
public sealed class LedgerEntry : Entity<Guid>
{
    public AccountId AccountId { get; private init; } = null!;
    public LedgerAccountType LedgerAccount { get; private init; }
    public Money? DebitAmount { get; private init; }
    public Money? CreditAmount { get; private init; }
    public DateTime TransactionDate { get; private init; }
    public SourceType SourceType { get; private init; }
    public string SourceReferenceId { get; private init; } = string.Empty;
    public string? Metadata { get; private init; }
    public DateTime CreatedAtUtc { get; private init; }
    public string CreatedBy { get; private init; } = string.Empty;

    private LedgerEntry()
    {
    }

    /// <summary>
    /// Creates a debit ledger entry
    /// </summary>
    public static LedgerEntry CreateDebit(
        AccountId accountId,
        LedgerAccountType ledgerAccount,
        Money amount,
        SourceType sourceType,
        string sourceReferenceId,
        DateTime transactionDate,
        Guid tenantId,
        string createdBy,
        string? metadata = null)
    {
        ValidateAmount(amount);
        ValidateSourceReference(sourceReferenceId);

        return new LedgerEntry
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            LedgerAccount = ledgerAccount,
            DebitAmount = amount,
            CreditAmount = null,
            TransactionDate = transactionDate,
            SourceType = sourceType,
            SourceReferenceId = sourceReferenceId,
            Metadata = metadata,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Creates a credit ledger entry
    /// </summary>
    public static LedgerEntry CreateCredit(
        AccountId accountId,
        LedgerAccountType ledgerAccount,
        Money amount,
        SourceType sourceType,
        string sourceReferenceId,
        DateTime transactionDate,
        Guid tenantId,
        string createdBy,
        string? metadata = null)
    {
        ValidateAmount(amount);
        ValidateSourceReference(sourceReferenceId);

        return new LedgerEntry
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            LedgerAccount = ledgerAccount,
            DebitAmount = null,
            CreditAmount = amount,
            TransactionDate = transactionDate,
            SourceType = sourceType,
            SourceReferenceId = sourceReferenceId,
            Metadata = metadata,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    private static void ValidateAmount(Money amount)
    {
        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }
    }

    private static void ValidateSourceReference(string sourceReferenceId)
    {
        if (string.IsNullOrWhiteSpace(sourceReferenceId))
        {
            throw new ArgumentException("Source reference ID cannot be empty", nameof(sourceReferenceId));
        }
    }

    /// <summary>
    /// Gets the effective amount (debit - credit) for balance calculation
    /// </summary>
    public Money GetEffectiveAmount()
    {
        if (DebitAmount is not null)
        {
            return DebitAmount;
        }

        if (CreditAmount is not null)
        {
            return Money.Create(-CreditAmount.Amount, CreditAmount.Currency);
        }

        throw new InvalidOperationException("Ledger entry must have either debit or credit amount");
    }
}
