using RideLedger.Domain.Enums;
using RideLedger.Domain.Primitives;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Events;

/// <summary>
/// Domain event raised when an account is created
/// </summary>
public sealed record AccountCreatedEvent : DomainEvent
{
    public AccountId AccountId { get; init; }
    public string Name { get; init; }
    public AccountType Type { get; init; }

    public AccountCreatedEvent(AccountId accountId, string name, AccountType type, Guid tenantId)
        : base(tenantId)
    {
        AccountId = accountId;
        Name = name;
        Type = type;
    }
}

/// <summary>
/// Domain event raised when a charge is recorded
/// </summary>
public sealed record ChargeRecordedEvent : DomainEvent
{
    public AccountId AccountId { get; init; }
    public RideId RideId { get; init; }
    public Money Amount { get; init; }
    public DateTime ServiceDate { get; init; }
    public string FleetId { get; init; }

    public ChargeRecordedEvent(
        AccountId accountId,
        RideId rideId,
        Money amount,
        DateTime serviceDate,
        string fleetId,
        Guid tenantId)
        : base(tenantId)
    {
        AccountId = accountId;
        RideId = rideId;
        Amount = amount;
        ServiceDate = serviceDate;
        FleetId = fleetId;
    }
}

/// <summary>
/// Domain event raised when a payment is recorded
/// </summary>
public sealed record PaymentReceivedEvent : DomainEvent
{
    public AccountId AccountId { get; init; }
    public PaymentReferenceId PaymentReferenceId { get; init; }
    public Money Amount { get; init; }
    public DateTime PaymentDate { get; init; }
    public PaymentMode? PaymentMode { get; init; }

    public PaymentReceivedEvent(
        AccountId accountId,
        PaymentReferenceId paymentReferenceId,
        Money amount,
        DateTime paymentDate,
        PaymentMode? paymentMode,
        Guid tenantId)
        : base(tenantId)
    {
        AccountId = accountId;
        PaymentReferenceId = paymentReferenceId;
        Amount = amount;
        PaymentDate = paymentDate;
        PaymentMode = paymentMode;
    }
}

/// <summary>
/// Domain event raised when an account is deactivated
/// </summary>
public sealed record AccountDeactivatedEvent : DomainEvent
{
    public AccountId AccountId { get; init; }
    public string Reason { get; init; }

    public AccountDeactivatedEvent(AccountId accountId, string reason, Guid tenantId)
        : base(tenantId)
    {
        AccountId = accountId;
        Reason = reason;
    }
}

/// <summary>
/// Domain event raised when an invoice is generated
/// </summary>
public sealed record InvoiceGeneratedEvent : DomainEvent
{
    public string InvoiceNumber { get; init; }
    public AccountId AccountId { get; init; }
    public DateTime BillingPeriodStart { get; init; }
    public DateTime BillingPeriodEnd { get; init; }
    public Money TotalAmount { get; init; }
    public Money OutstandingBalance { get; init; }

    public InvoiceGeneratedEvent(
        string invoiceNumber,
        AccountId accountId,
        DateTime billingPeriodStart,
        DateTime billingPeriodEnd,
        Money totalAmount,
        Money outstandingBalance,
        Guid tenantId)
        : base(tenantId)
    {
        InvoiceNumber = invoiceNumber;
        AccountId = accountId;
        BillingPeriodStart = billingPeriodStart;
        BillingPeriodEnd = billingPeriodEnd;
        TotalAmount = totalAmount;
        OutstandingBalance = outstandingBalance;
    }
}
