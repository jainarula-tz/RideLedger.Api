using FluentResults;
using RideLedger.Domain.Entities;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Errors;
using RideLedger.Domain.Events;
using RideLedger.Domain.Primitives;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Aggregates;

/// <summary>
/// Account aggregate root representing a financially responsible entity
/// Enforces double-entry accounting invariants and idempotency
/// </summary>
public sealed class Account : AggregateRoot<AccountId>
{
    private readonly List<LedgerEntry> _ledgerEntries = new();

    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private init; }
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the read-only collection of ledger entries
    /// </summary>
    public IReadOnlyCollection<LedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    private Account()
    {
    }

    /// <summary>
    /// Factory method to create a new account
    /// </summary>
    public static Account Create(
        AccountId id,
        string name,
        AccountType type,
        Guid tenantId)
    {
        ValidateName(name);

        var account = new Account
        {
            Id = id,
            Name = name.Trim(),
            Type = type,
            Status = AccountStatus.Active,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow
        };

        account.RaiseDomainEvent(new AccountCreatedEvent(id, name, type, tenantId));

        return account;
    }

    /// <summary>
    /// Records a ride service charge creating double-entry ledger entries
    /// Enforces: idempotency, active status, positive amount
    /// </summary>
    public Result RecordCharge(
        RideId rideId,
        Money amount,
        DateTime serviceDate,
        string fleetId,
        string createdBy)
    {
        // Validate account is active
        if (Status != AccountStatus.Active)
        {
            return Result.Fail(AccountErrors.Inactive(Id));
        }

        // Validate amount is positive
        if (amount.Amount <= 0)
        {
            return Result.Fail(LedgerErrors.InvalidAmount(amount.Amount));
        }

        // Check for duplicate charge (idempotency)
        bool isDuplicate = _ledgerEntries.Any(e =>
            e.SourceType == SourceType.Ride &&
            e.SourceReferenceId == rideId.Value);

        if (isDuplicate)
        {
            return Result.Fail(LedgerErrors.DuplicateCharge(rideId, Id));
        }

        // Create double-entry ledger entries
        var metadata = System.Text.Json.JsonSerializer.Serialize(new { FleetId = fleetId });

        // Debit: Accounts Receivable (Asset increases)
        var debitEntry = LedgerEntry.CreateDebit(
            accountId: Id,
            ledgerAccount: LedgerAccountType.AccountsReceivable,
            amount: amount,
            sourceType: SourceType.Ride,
            sourceReferenceId: rideId.Value,
            transactionDate: serviceDate,
            tenantId: TenantId,
            createdBy: createdBy,
            metadata: metadata);

        // Credit: Service Revenue (Revenue increases)
        var creditEntry = LedgerEntry.CreateCredit(
            accountId: Id,
            ledgerAccount: LedgerAccountType.ServiceRevenue,
            amount: amount,
            sourceType: SourceType.Ride,
            sourceReferenceId: rideId.Value,
            transactionDate: serviceDate,
            tenantId: TenantId,
            createdBy: createdBy,
            metadata: metadata);

        _ledgerEntries.Add(debitEntry);
        _ledgerEntries.Add(creditEntry);
        UpdatedAtUtc = DateTime.UtcNow;

        // Raise domain event
        RaiseDomainEvent(new ChargeRecordedEvent(
            Id, rideId, amount, serviceDate, fleetId, TenantId));

        return Result.Ok();
    }

    /// <summary>
    /// Records a payment creating double-entry ledger entries
    /// Enforces: idempotency, active status, positive amount
    /// Supports: partial payments, full payments, overpayments
    /// </summary>
    public Result RecordPayment(
        PaymentReferenceId paymentReferenceId,
        Money amount,
        DateTime paymentDate,
        PaymentMode? paymentMode,
        string createdBy)
    {
        // Validate account is active
        if (Status != AccountStatus.Active)
        {
            return Result.Fail(AccountErrors.Inactive(Id));
        }

        // Validate amount is positive
        if (amount.Amount <= 0)
        {
            return Result.Fail(LedgerErrors.InvalidAmount(amount.Amount));
        }

        // Check for duplicate payment (idempotency)
        bool isDuplicate = _ledgerEntries.Any(e =>
            e.SourceType == SourceType.Payment &&
            e.SourceReferenceId == paymentReferenceId.Value);

        if (isDuplicate)
        {
            return Result.Fail(LedgerErrors.DuplicatePayment(paymentReferenceId));
        }

        // Create double-entry ledger entries
        var metadata = paymentMode.HasValue
            ? System.Text.Json.JsonSerializer.Serialize(new { PaymentMode = paymentMode.Value.ToString() })
            : null;

        // Debit: Cash/Bank (Asset increases)
        var debitEntry = LedgerEntry.CreateDebit(
            accountId: Id,
            ledgerAccount: LedgerAccountType.Cash,
            amount: amount,
            sourceType: SourceType.Payment,
            sourceReferenceId: paymentReferenceId.Value,
            transactionDate: paymentDate,
            tenantId: TenantId,
            createdBy: createdBy,
            metadata: metadata);

        // Credit: Accounts Receivable (Asset decreases)
        var creditEntry = LedgerEntry.CreateCredit(
            accountId: Id,
            ledgerAccount: LedgerAccountType.AccountsReceivable,
            amount: amount,
            sourceType: SourceType.Payment,
            sourceReferenceId: paymentReferenceId.Value,
            transactionDate: paymentDate,
            tenantId: TenantId,
            createdBy: createdBy,
            metadata: metadata);

        _ledgerEntries.Add(debitEntry);
        _ledgerEntries.Add(creditEntry);
        UpdatedAtUtc = DateTime.UtcNow;

        // Raise domain event
        RaiseDomainEvent(new PaymentReceivedEvent(
            Id, paymentReferenceId, amount, paymentDate, paymentMode, TenantId));

        return Result.Ok();
    }

    /// <summary>
    /// Calculates current balance: Total Debits - Total Credits on AR account
    /// </summary>
    public Money GetBalance()
    {
        var receivableEntries = _ledgerEntries
            .Where(e => e.LedgerAccount == LedgerAccountType.AccountsReceivable);

        decimal totalDebits = receivableEntries
            .Where(e => e.DebitAmount is not null)
            .Sum(e => e.DebitAmount!.Amount);

        decimal totalCredits = receivableEntries
            .Where(e => e.CreditAmount is not null)
            .Sum(e => e.CreditAmount!.Amount);

        decimal balance = totalDebits - totalCredits;

        return Money.Create(balance, Money.DefaultCurrency);
    }

    /// <summary>
    /// Deactivates the account preventing further transactions
    /// </summary>
    public Result Deactivate(string reason)
    {
        if (Status == AccountStatus.Inactive)
        {
            return Result.Ok(); // Already inactive
        }

        Status = AccountStatus.Inactive;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new AccountDeactivatedEvent(Id, reason, TenantId));

        return Result.Ok();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Account name cannot be empty", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Account name cannot exceed 200 characters", nameof(name));
        }
    }
}
