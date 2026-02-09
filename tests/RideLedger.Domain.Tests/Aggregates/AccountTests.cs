using FluentAssertions;
using FluentResults;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Errors;
using RideLedger.Domain.Events;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Tests.Aggregates;

public class AccountTests
{
    private readonly AccountId _accountId = AccountId.Create(Guid.NewGuid());
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string AccountName = "Test Account";
    private const string CreatedBy = "test-user";
    private const string FleetId = "fleet-123";

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldReturnAccount()
    {
        // Act
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);

        // Assert
        account.Should().NotBeNull();
        account.Id.Should().Be(_accountId);
        account.Name.Should().Be(AccountName);
        account.Type.Should().Be(AccountType.Organization);
        account.Status.Should().Be(AccountStatus.Active);
        account.TenantId.Should().Be(_tenantId);
        account.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        account.LedgerEntries.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValidParameters_ShouldRaiseAccountCreatedEvent()
    {
        // Act
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);

        // Assert
        account.DomainEvents.Should().ContainSingle();
        var domainEvent = account.DomainEvents.First();
        domainEvent.Should().BeOfType<AccountCreatedEvent>();

        var accountCreatedEvent = (AccountCreatedEvent)domainEvent;
        accountCreatedEvent.AccountId.Should().Be(_accountId);
        accountCreatedEvent.Name.Should().Be(AccountName);
        accountCreatedEvent.Type.Should().Be(AccountType.Organization);
        accountCreatedEvent.TenantId.Should().Be(_tenantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => Account.Create(_accountId, invalidName!, AccountType.Organization, _tenantId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Account name cannot be empty*");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act
        var act = () => Account.Create(_accountId, longName, AccountType.Organization, _tenantId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Account name cannot exceed 200 characters*");
    }

    [Fact]
    public void Create_WithNameWithWhitespace_ShouldTrimName()
    {
        // Arrange
        var nameWithWhitespace = "  Test Account  ";

        // Act
        var account = Account.Create(_accountId, nameWithWhitespace, AccountType.Organization, _tenantId);

        // Assert
        account.Name.Should().Be("Test Account");
    }

    #endregion

    #region RecordCharge Tests

    [Fact]
    public void RecordCharge_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(100.00m, "USD");
        var serviceDate = DateTime.UtcNow.Date;

        // Act
        var result = account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.LedgerEntries.Should().HaveCount(2); // Double-entry: Debit AR, Credit Revenue
        account.UpdatedAtUtc.Should().NotBeNull();
        account.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordCharge_WithValidParameters_ShouldCreateDoubleEntryLedger()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(100.00m, "USD");
        var serviceDate = DateTime.UtcNow.Date;

        // Act
        account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);

        // Assert
        var entries = account.LedgerEntries.ToList();
        entries.Should().HaveCount(2);

        // Debit entry: Accounts Receivable
        var debitEntry = entries.First(e => e.DebitAmount is not null);
        debitEntry.LedgerAccount.Should().Be(LedgerAccountType.AccountsReceivable);
        debitEntry.DebitAmount.Should().NotBeNull();
        debitEntry.DebitAmount!.Amount.Should().Be(100.00m);
        debitEntry.CreditAmount.Should().BeNull();
        debitEntry.SourceType.Should().Be(SourceType.Ride);
        debitEntry.SourceReferenceId.Should().Be(rideId.Value);
        debitEntry.TransactionDate.Should().Be(serviceDate);

        // Credit entry: Service Revenue
        var creditEntry = entries.First(e => e.CreditAmount is not null);
        creditEntry.LedgerAccount.Should().Be(LedgerAccountType.ServiceRevenue);
        creditEntry.CreditAmount.Should().NotBeNull();
        creditEntry.CreditAmount!.Amount.Should().Be(100.00m);
        creditEntry.DebitAmount.Should().BeNull();
        creditEntry.SourceType.Should().Be(SourceType.Ride);
        creditEntry.SourceReferenceId.Should().Be(rideId.Value);
        creditEntry.TransactionDate.Should().Be(serviceDate);
    }

    [Fact]
    public void RecordCharge_WithValidParameters_ShouldRaiseChargeRecordedEvent()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(100.00m, "USD");
        var serviceDate = DateTime.UtcNow.Date;

        // Act
        account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);

        // Assert
        account.DomainEvents.Should().HaveCount(2); // AccountCreated + ChargeRecorded
        var chargeEvent = account.DomainEvents.OfType<ChargeRecordedEvent>().FirstOrDefault();
        chargeEvent.Should().NotBeNull();
        chargeEvent!.AccountId.Should().Be(_accountId);
        chargeEvent.RideId.Should().Be(rideId);
        chargeEvent.Amount.Should().Be(amount);
        chargeEvent.ServiceDate.Should().Be(serviceDate);
        chargeEvent.FleetId.Should().Be(FleetId);
        chargeEvent.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void RecordCharge_OnInactiveAccount_ShouldFail()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        account.Deactivate("Test deactivation");
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(100.00m, "USD");
        var serviceDate = DateTime.UtcNow.Date;

        // Act
        var result = account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("inactive");
        result.Errors[0].Metadata["ErrorCode"].Should().Be("ACCOUNT_INACTIVE");
        account.LedgerEntries.Should().BeEmpty(); // No entries should be created
    }

    [Fact]
    public void RecordCharge_WithDuplicateRideId_ShouldFail()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(100.00m, "USD");
        var serviceDate = DateTime.UtcNow.Date;

        // Record charge first time
        account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);

        // Act - Try to record same charge again
        var result = account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("already been recorded");
        result.Errors[0].Metadata["ErrorCode"].Should().Be("LEDGER_DUPLICATE_CHARGE");
        account.LedgerEntries.Should().HaveCount(2); // Only the first charge's entries
    }

    [Fact]
    public void RecordCharge_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var serviceDate = DateTime.UtcNow.Date;

        // Act & Assert - Money.Create throws before domain validation
        var act = () => Money.Create(-100.00m, "USD");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Amount cannot be negative*");
    }

    [Fact]
    public void RecordCharge_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var amount = Money.Zero();
        var serviceDate = DateTime.UtcNow.Date;

        // Act
        var result = account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Metadata["ErrorCode"].Should().Be("LEDGER_INVALID_AMOUNT");
        account.LedgerEntries.Should().BeEmpty();
    }

    #endregion

    #region RecordPayment Tests

    [Fact]
    public void RecordPayment_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var paymentRef = PaymentReferenceId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(50.00m, "USD");
        var paymentDate = DateTime.UtcNow.Date;

        // Act
        var result = account.RecordPayment(paymentRef, amount, paymentDate, PaymentMode.Cash, CreatedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.LedgerEntries.Should().HaveCount(2); // Double-entry: Debit Cash, Credit AR
        account.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void RecordPayment_WithValidParameters_ShouldCreateDoubleEntryLedger()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var paymentRef = PaymentReferenceId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(50.00m, "USD");
        var paymentDate = DateTime.UtcNow.Date;

        // Act
        account.RecordPayment(paymentRef, amount, paymentDate, PaymentMode.Cash, CreatedBy);

        // Assert
        var entries = account.LedgerEntries.ToList();
        entries.Should().HaveCount(2);

        // Debit entry: Cash
        var debitEntry = entries.First(e => e.DebitAmount is not null);
        debitEntry.LedgerAccount.Should().Be(LedgerAccountType.Cash);
        debitEntry.DebitAmount.Should().NotBeNull();
        debitEntry.DebitAmount!.Amount.Should().Be(50.00m);
        debitEntry.SourceType.Should().Be(SourceType.Payment);
        debitEntry.SourceReferenceId.Should().Be(paymentRef.Value);

        // Credit entry: Accounts Receivable
        var creditEntry = entries.First(e => e.CreditAmount is not null);
        creditEntry.LedgerAccount.Should().Be(LedgerAccountType.AccountsReceivable);
        creditEntry.CreditAmount.Should().NotBeNull();
        creditEntry.CreditAmount!.Amount.Should().Be(50.00m);
        creditEntry.SourceType.Should().Be(SourceType.Payment);
        creditEntry.SourceReferenceId.Should().Be(paymentRef.Value);
    }

    [Fact]
    public void RecordPayment_WithValidParameters_ShouldRaisePaymentReceivedEvent()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var paymentRef = PaymentReferenceId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(50.00m, "USD");
        var paymentDate = DateTime.UtcNow.Date;

        // Act
        account.RecordPayment(paymentRef, amount, paymentDate, PaymentMode.Cash, CreatedBy);

        // Assert
        account.DomainEvents.Should().HaveCount(2); // AccountCreated + PaymentReceived
        var paymentEvent = account.DomainEvents.OfType<PaymentReceivedEvent>().FirstOrDefault();
        paymentEvent.Should().NotBeNull();
        paymentEvent!.AccountId.Should().Be(_accountId);
        paymentEvent.PaymentReferenceId.Should().Be(paymentRef);
        paymentEvent.Amount.Should().Be(amount);
        paymentEvent.PaymentDate.Should().Be(paymentDate);
        paymentEvent.PaymentMode.Should().Be(PaymentMode.Cash);
    }

    [Fact]
    public void RecordPayment_OnInactiveAccount_ShouldFail()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        account.Deactivate("Test deactivation");
        var paymentRef = PaymentReferenceId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(50.00m, "USD");
        var paymentDate = DateTime.UtcNow.Date;

        // Act
        var result = account.RecordPayment(paymentRef, amount, paymentDate, PaymentMode.Cash, CreatedBy);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Metadata["ErrorCode"].Should().Be("ACCOUNT_INACTIVE");
        account.LedgerEntries.Should().BeEmpty();
    }

    [Fact]
    public void RecordPayment_WithDuplicatePaymentReference_ShouldFail()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var paymentRef = PaymentReferenceId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(50.00m, "USD");
        var paymentDate = DateTime.UtcNow.Date;

        // Record payment first time
        account.RecordPayment(paymentRef, amount, paymentDate, PaymentMode.Cash, CreatedBy);

        // Act - Try to record same payment again
        var result = account.RecordPayment(paymentRef, amount, paymentDate, PaymentMode.Cash, CreatedBy);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Metadata["ErrorCode"].Should().Be("LEDGER_DUPLICATE_PAYMENT");
        account.LedgerEntries.Should().HaveCount(2); // Only the first payment's entries
    }

    [Fact]
    public void RecordPayment_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var paymentRef = PaymentReferenceId.Create(Guid.NewGuid().ToString());
        var paymentDate = DateTime.UtcNow.Date;

        // Act & Assert - Money.Create throws before domain validation
        var act = () => Money.Create(-50.00m, "USD");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Amount cannot be negative*");
    }

    [Fact]
    public void RecordPayment_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var paymentRef = PaymentReferenceId.Create(Guid.NewGuid().ToString());
        var amount = Money.Zero();
        var paymentDate = DateTime.UtcNow.Date;

        // Act
        var result = account.RecordPayment(paymentRef, amount, paymentDate, null, CreatedBy);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Metadata["ErrorCode"].Should().Be("LEDGER_INVALID_AMOUNT");
        account.LedgerEntries.Should().BeEmpty();
    }

    #endregion

    #region GetBalance Tests

    [Fact]
    public void GetBalance_OnNewAccount_ShouldReturnZero()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);

        // Act
        var balance = account.GetBalance();

        // Assert
        balance.Amount.Should().Be(0);
        balance.Currency.Should().Be("USD");
    }

    [Fact]
    public void GetBalance_AfterRecordingCharge_ShouldReturnPositiveBalance()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var rideId = RideId.Create(Guid.NewGuid().ToString());
        var amount = Money.Create(100.00m, "USD");
        var serviceDate = DateTime.UtcNow.Date;

        // Act
        account.RecordCharge(rideId, amount, serviceDate, FleetId, CreatedBy);
        var balance = account.GetBalance();

        // Assert
        balance.Amount.Should().Be(100.00m);
    }

    [Fact]
    public void GetBalance_AfterMultipleCharges_ShouldReturnCorrectSum()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var serviceDate = DateTime.UtcNow.Date;

        // Act - Record 3 charges
        account.RecordCharge(RideId.Create(Guid.NewGuid().ToString()), Money.Create(100.00m, "USD"), serviceDate, FleetId, CreatedBy);
        account.RecordCharge(RideId.Create(Guid.NewGuid().ToString()), Money.Create(75.50m, "USD"), serviceDate, FleetId, CreatedBy);
        account.RecordCharge(RideId.Create(Guid.NewGuid().ToString()), Money.Create(50.25m, "USD"), serviceDate, FleetId, CreatedBy);

        var balance = account.GetBalance();

        // Assert
        balance.Amount.Should().Be(225.75m); // 100 + 75.50 + 50.25
    }

    [Fact]
    public void GetBalance_AfterChargesAndPayments_ShouldReturnOutstandingBalance()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var date = DateTime.UtcNow.Date;

        // Act - Record charges totaling 300.00
        account.RecordCharge(RideId.Create(Guid.NewGuid().ToString()), Money.Create(150.00m, "USD"), date, FleetId, CreatedBy);
        account.RecordCharge(RideId.Create(Guid.NewGuid().ToString()), Money.Create(150.00m, "USD"), date, FleetId, CreatedBy);

        // Record payments totaling 220.00
        account.RecordPayment(PaymentReferenceId.Create(Guid.NewGuid().ToString()), Money.Create(100.00m, "USD"), date, PaymentMode.Cash, CreatedBy);
        account.RecordPayment(PaymentReferenceId.Create(Guid.NewGuid().ToString()), Money.Create(120.00m, "USD"), date, PaymentMode.BankTransfer, CreatedBy);

        var balance = account.GetBalance();

        // Assert
        balance.Amount.Should().Be(80.00m); // 300 - 220
    }

    [Fact]
    public void GetBalance_AfterFullPayment_ShouldReturnZero()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var date = DateTime.UtcNow.Date;
        var chargeAmount = Money.Create(100.00m, "USD");

        // Act
        account.RecordCharge(RideId.Create(Guid.NewGuid().ToString()), chargeAmount, date, FleetId, CreatedBy);
        account.RecordPayment(PaymentReferenceId.Create(Guid.NewGuid().ToString()), chargeAmount, date, PaymentMode.Cash, CreatedBy);

        var balance = account.GetBalance();

        // Assert
        balance.Amount.Should().Be(0);
    }

    [Fact]
    public void GetBalance_AfterPartialPayment_ShouldReturnRemainingBalance()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        var date = DateTime.UtcNow.Date;

        // Act - Charge 100, Pay 60
        account.RecordCharge(RideId.Create(Guid.NewGuid().ToString()), Money.Create(100.00m, "USD"), date, FleetId, CreatedBy);
        account.RecordPayment(PaymentReferenceId.Create(Guid.NewGuid().ToString()), Money.Create(60.00m, "USD"), date, PaymentMode.Cash, CreatedBy);

        var balance = account.GetBalance();

        // Assert
        balance.Amount.Should().Be(40.00m); // 100 - 60 = 40 remaining
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public void Deactivate_OnActiveAccount_ShouldSucceed()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        const string reason = "Account closed by customer request";

        // Act
        var result = account.Deactivate(reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.Inactive);
        account.UpdatedAtUtc.Should().NotBeNull();
        account.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_OnActiveAccount_ShouldRaiseAccountDeactivatedEvent()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        const string reason = "Test deactivation";

        // Act
        account.Deactivate(reason);

        // Assert
        account.DomainEvents.Should().HaveCount(2); // AccountCreated + AccountDeactivated
        var deactivatedEvent = account.DomainEvents.OfType<AccountDeactivatedEvent>().FirstOrDefault();
        deactivatedEvent.Should().NotBeNull();
        deactivatedEvent!.AccountId.Should().Be(_accountId);
        deactivatedEvent.Reason.Should().Be(reason);
        deactivatedEvent.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Deactivate_OnInactiveAccount_ShouldSucceedIdempotently()
    {
        // Arrange
        var account = Account.Create(_accountId, AccountName, AccountType.Organization, _tenantId);
        account.Deactivate("First deactivation");

        // Act - Deactivate again
        var result = account.Deactivate("Second deactivation");

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.Inactive);
        // Should only have 2 events, not 3 (no duplicate deactivation event)
        account.DomainEvents.Should().HaveCount(2);
    }

    #endregion
}
