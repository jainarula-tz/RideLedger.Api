using FluentAssertions;
using NSubstitute;
using RideLedger.Application.DTOs.Balances;
using RideLedger.Application.Handlers.Balances;
using RideLedger.Application.Queries.Balances;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;
using Xunit;

namespace RideLedger.Application.Tests.Handlers.Balances;

/// <summary>
/// Unit tests for GetAccountBalanceQueryHandler
/// Tests balance calculation, error handling, and repository interactions
/// </summary>
public sealed class GetAccountBalanceQueryHandlerTests
{
    private readonly IAccountRepository _accountRepository;
    private readonly GetAccountBalanceQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetAccountBalanceQueryHandlerTests()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _handler = new GetAccountBalanceQueryHandler(_accountRepository);
    }

    [Fact]
    public async Task Handle_WithValidAccount_ShouldReturnBalanceResponse()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Test Account",
            AccountType.Organization,
            _tenantId);

        // Record a charge
        account.RecordCharge(
            RideId.Create("ride-123"),
            Money.Create(150.00m),
            DateTime.UtcNow.Date,
            "fleet-456",
            _tenantId.ToString());

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Is<AccountId>(id => id.Value == accountId),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var query = new GetAccountBalanceQuery { AccountId = accountId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccountId.Should().Be(accountId);
        result.Value.Balance.Should().Be(150.00m, "because one charge of 150.00 was recorded");
        result.Value.Currency.Should().Be("USD");
        result.Value.AsOfDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        await _accountRepository.Received(1).GetByIdWithLedgerEntriesAsync(
            Arg.Is<AccountId>(id => id.Value == accountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForNonExistentAccount_ShouldReturnFailureResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var query = new GetAccountBalanceQuery { AccountId = accountId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("not found"));

        await _accountRepository.Received(1).GetByIdWithLedgerEntriesAsync(
            Arg.Is<AccountId>(id => id.Value == accountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithZeroBalance_ShouldReturnZeroAmount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "New Account",
            AccountType.Organization,
            _tenantId);

        // No transactions recorded

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var query = new GetAccountBalanceQuery { AccountId = accountId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(0.00m, "because account has no transactions");
        result.Value.AccountId.Should().Be(accountId);
    }

    [Fact]
    public async Task Handle_WithMultipleCharges_ShouldReturnCorrectBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Multi-Transaction Account",
            AccountType.Organization,
            _tenantId);

        // Record multiple charges
        account.RecordCharge(
            RideId.Create("ride-001"),
            Money.Create(100.00m),
            DateTime.UtcNow.Date,
            "fleet-1",
            _tenantId.ToString());

        account.RecordCharge(
            RideId.Create("ride-002"),
            Money.Create(250.50m),
            DateTime.UtcNow.Date,
            "fleet-1",
            _tenantId.ToString());

        account.RecordCharge(
            RideId.Create("ride-003"),
            Money.Create(75.25m),
            DateTime.UtcNow.Date,
            "fleet-2",
            _tenantId.ToString());

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var query = new GetAccountBalanceQuery { AccountId = accountId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(425.75m, "because total charges are 100.00 + 250.50 + 75.25");
    }

    [Fact]
    public async Task Handle_WithChargesAndPayments_ShouldReturnRemainingBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Account With Payments",
            AccountType.Organization,
            _tenantId);

        // Record charges
        account.RecordCharge(
            RideId.Create("ride-100"),
            Money.Create(500.00m),
            DateTime.UtcNow.Date,
            "fleet-x",
            _tenantId.ToString());

        account.RecordCharge(
            RideId.Create("ride-200"),
            Money.Create(300.00m),
            DateTime.UtcNow.Date,
            "fleet-y",
            _tenantId.ToString());

        // Record payment
        account.RecordPayment(
            PaymentReferenceId.Create("payment-001"),
            Money.Create(600.00m),
            DateTime.UtcNow.Date,
            PaymentMode.BankTransfer,
            _tenantId.ToString());

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var query = new GetAccountBalanceQuery { AccountId = accountId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(200.00m, "because balance is 800.00 (charges) - 600.00 (payment)");
    }

    [Fact(Skip = "Domain constraint: Money value object does not allow negative amounts - overpayment scenario needs domain model update")]
    public async Task Handle_WithOverpayment_ShouldReturnNegativeBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Overpaid Account",
            AccountType.Organization,
            _tenantId);

        // Record charge
        account.RecordCharge(
            RideId.Create("ride-999"),
            Money.Create(100.00m),
            DateTime.UtcNow.Date,
            "fleet-z",
            _tenantId.ToString());

        // Record payment exceeding charges
        account.RecordPayment(
            PaymentReferenceId.Create("payment-large"),
            Money.Create(250.00m),
            DateTime.UtcNow.Date,
            PaymentMode.Cash,
            _tenantId.ToString());

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var query = new GetAccountBalanceQuery { AccountId = accountId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(-150.00m, "because payment of 250.00 exceeds charge of 100.00");
    }
}
