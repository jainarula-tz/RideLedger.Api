using FluentAssertions;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Entities;
using RideLedger.Domain.Enums;
using RideLedger.Domain.ValueObjects;
using Xunit;

namespace RideLedger.Domain.Tests.Aggregates;

/// <summary>
/// Unit tests for Invoice aggregate
/// Tests invoice generation, validation, and business rules
/// </summary>
public sealed class InvoiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly AccountId _accountId;

    public InvoiceTests()
    {
        _accountId = AccountId.Create(Guid.NewGuid());
    }

    [Fact]
    public void Generate_WithValidInputs_ShouldCreateInvoice()
    {
        // Arrange
        var invoiceNumber = "INV-2024-001";
        var billingPeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var lineItems = CreateLineItems(3);
        var totalPayments = Money.Create(0m);

        // Act
        var result = Invoice.Generate(
            invoiceNumber,
            _accountId,
            BillingFrequency.Monthly,
            billingPeriodStart,
            billingPeriodEnd,
            lineItems,
            totalPayments,
            _tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invoice = result.Value;

        invoice.InvoiceNumber.Should().Be(invoiceNumber);
        invoice.AccountId.Should().Be(_accountId);
        invoice.BillingFrequency.Should().Be(BillingFrequency.Monthly);
        invoice.BillingPeriodStart.Should().Be(billingPeriodStart);
        invoice.BillingPeriodEnd.Should().Be(billingPeriodEnd);
        invoice.Status.Should().Be(InvoiceStatus.Generated);
        invoice.LineItems.Should().HaveCount(3);
        invoice.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        invoice.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Generate_WithMultipleLineItems_ShouldCalculateCorrectSubtotal()
    {
        // Arrange
        var invoiceNumber = "INV-2024-002";
        var billingPeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var lineItems = new[]
        {
            InvoiceLineItem.Create(Guid.Empty, "ride-001", DateTime.UtcNow, Money.Create(100.50m), "Ride 1", new[] { Guid.NewGuid() }),
            InvoiceLineItem.Create(Guid.Empty, "ride-002", DateTime.UtcNow, Money.Create(250.75m), "Ride 2", new[] { Guid.NewGuid() }),
            InvoiceLineItem.Create(Guid.Empty, "ride-003", DateTime.UtcNow, Money.Create(75.25m), "Ride 3", new[] { Guid.NewGuid() })
        };

        var totalPayments = Money.Create(0m);

        // Act
        var result = Invoice.Generate(
            invoiceNumber,
            _accountId,
            BillingFrequency.Monthly,
            billingPeriodStart,
            billingPeriodEnd,
            lineItems,
            totalPayments,
            _tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invoice = result.Value;

        invoice.Subtotal.Amount.Should().Be(426.50m, "because 100.50 + 250.75 + 75.25 = 426.50");
        invoice.TotalPaymentsApplied.Amount.Should().Be(0m);
        invoice.OutstandingBalance.Amount.Should().Be(426.50m);
    }

    [Fact]
    public void Generate_WithPaymentsApplied_ShouldCalculateOutstandingBalance()
    {
        // Arrange
        var invoiceNumber = "INV-2024-003";
        var billingPeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var lineItems = new[]
        {
            InvoiceLineItem.Create(Guid.Empty, "ride-001", DateTime.UtcNow, Money.Create(500.00m), "Ride 1", new[] { Guid.NewGuid() }),
            InvoiceLineItem.Create(Guid.Empty, "ride-002", DateTime.UtcNow, Money.Create(300.00m), "Ride 2", new[] { Guid.NewGuid() })
        };

        var totalPayments = Money.Create(600.00m);

        // Act
        var result = Invoice.Generate(
            invoiceNumber,
            _accountId,
            BillingFrequency.Monthly,
            billingPeriodStart,
            billingPeriodEnd,
            lineItems,
            totalPayments,
            _tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invoice = result.Value;

        invoice.Subtotal.Amount.Should().Be(800.00m);
        invoice.TotalPaymentsApplied.Amount.Should().Be(600.00m);
        invoice.OutstandingBalance.Amount.Should().Be(200.00m, "because 800.00 - 600.00 = 200.00");
    }

    [Fact]
    public void Generate_WithEmptyInvoiceNumber_ShouldReturnFailure()
    {
        // Arrange
        var invoiceNumber = "";
        var billingPeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var lineItems = CreateLineItems(1);
        var totalPayments = Money.Create(0m);

        // Act
        var result = Invoice.Generate(
            invoiceNumber,
            _accountId,
            BillingFrequency.Monthly,
            billingPeriodStart,
            billingPeriodEnd,
            lineItems,
            totalPayments,
            _tenantId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Invoice number is required"));
    }

    [Fact]
    public void Generate_WithInvalidBillingPeriod_ShouldReturnFailure()
    {
        // Arrange
        var invoiceNumber = "INV-2024-004";
        var billingPeriodStart = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);  // End before start
        var lineItems = CreateLineItems(1);
        var totalPayments = Money.Create(0m);

        // Act
        var result = Invoice.Generate(
            invoiceNumber,
            _accountId,
            BillingFrequency.Monthly,
            billingPeriodStart,
            billingPeriodEnd,
            lineItems,
            totalPayments,
            _tenantId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Billing period start must be before end date"));
    }

    [Fact]
    public void Generate_WithNoLineItems_ShouldReturnFailure()
    {
        // Arrange
        var invoiceNumber = "INV-2024-005";
        var billingPeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var lineItems = Enumerable.Empty<InvoiceLineItem>();
        var totalPayments = Money.Create(0m);

        // Act
        var result = Invoice.Generate(
            invoiceNumber,
            _accountId,
            BillingFrequency.Monthly,
            billingPeriodStart,
            billingPeriodEnd,
            lineItems,
            totalPayments,
            _tenantId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Invoice must have at least one line item"));
    }

    [Fact]
    public void Generate_WithDifferentBillingFrequencies_ShouldSucceed()
    {
        // Arrange
        var invoiceNumber = "INV-2024-006";
        var billingPeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var lineItems = CreateLineItems(1);
        var totalPayments = Money.Create(0m);

        var frequencies = new[]
        {
            BillingFrequency.PerRide,
            BillingFrequency.Daily,
            BillingFrequency.Weekly,
            BillingFrequency.Monthly
        };

        foreach (var frequency in frequencies)
        {
            // Act
            var result = Invoice.Generate(
                $"{invoiceNumber}-{frequency}",
                _accountId,
                frequency,
                billingPeriodStart,
                billingPeriodEnd,
                lineItems,
                totalPayments,
                _tenantId);

            // Assert
            result.IsSuccess.Should().BeTrue($"because {frequency} is a valid billing frequency");
            result.Value.BillingFrequency.Should().Be(frequency);
        }
    }

    [Fact]
    public void Void_ValidInvoice_ShouldMarkAsVoided()
    {
        // Arrange
        var lineItems = CreateLineItems(1);
        var result = Invoice.Generate(
            "INV-2024-007",
            _accountId,
            BillingFrequency.Monthly,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            lineItems,
            Money.Create(0m),
            _tenantId);

        var invoice = result.Value;

        // Act
        var voidResult = invoice.Void();

        // Assert
        voidResult.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Voided);
    }

    [Fact]
    public void Void_AlreadyVoidedInvoice_ShouldReturnFailure()
    {
        // Arrange
        var lineItems = CreateLineItems(1);
        var result = Invoice.Generate(
            "INV-2024-008",
            _accountId,
            BillingFrequency.Monthly,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            lineItems,
            Money.Create(0m),
            _tenantId);

        var invoice = result.Value;
        invoice.Void();  // Void once

        // Act
        var secondVoidResult = invoice.Void();  // Try to void again

        // Assert
        secondVoidResult.IsFailed.Should().BeTrue();
        secondVoidResult.Errors.Should().ContainSingle(e => e.Message.Contains("already voided"));
    }

    [Fact]
    public void Generate_WithLineItemTraceability_ShouldPreserveLedgerEntryIds()
    {
        // Arrange
        var invoiceNumber = "INV-2024-009";
        var billingPeriodStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var billingPeriodEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var ledgerEntryId1 = Guid.NewGuid();
        var ledgerEntryId2 = Guid.NewGuid();
        var ledgerEntryId3 = Guid.NewGuid();

        var lineItems = new[]
        {
            InvoiceLineItem.Create(Guid.Empty, "ride-001", DateTime.UtcNow, Money.Create(100.00m), "Ride 1", new[] { ledgerEntryId1, ledgerEntryId2 }),
            InvoiceLineItem.Create(Guid.Empty, "ride-002", DateTime.UtcNow, Money.Create(200.00m), "Ride 2", new[] { ledgerEntryId3 })
        };

        var totalPayments = Money.Create(0m);

        // Act
        var result = Invoice.Generate(
            invoiceNumber,
            _accountId,
            BillingFrequency.Monthly,
            billingPeriodStart,
            billingPeriodEnd,
            lineItems,
            totalPayments,
            _tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invoice = result.Value;

        invoice.LineItems.Should().HaveCount(2);
        invoice.LineItems.ElementAt(0).LedgerEntryIds.Should().Contain(new[] { ledgerEntryId1, ledgerEntryId2 });
        invoice.LineItems.ElementAt(1).LedgerEntryIds.Should().Contain(ledgerEntryId3);
    }

    // Helper method to create sample line items
    private List<InvoiceLineItem> CreateLineItems(int count)
    {
        var lineItems = new List<InvoiceLineItem>();

        for (int i = 1; i <= count; i++)
        {
            var lineItem = InvoiceLineItem.Create(
                Guid.Empty,
                $"ride-{i:D3}",
                DateTime.UtcNow.Date,
                Money.Create(100.00m * i),
                $"Ride {i}",
                new[] { Guid.NewGuid() });

            lineItems.Add(lineItem);
        }

        return lineItems;
    }
}
