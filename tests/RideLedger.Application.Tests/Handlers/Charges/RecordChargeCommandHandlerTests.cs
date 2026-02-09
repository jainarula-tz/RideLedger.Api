using FluentAssertions;
using FluentResults;
using NSubstitute;
using RideLedger.Application.Commands.Charges;
using RideLedger.Application.Common;
using RideLedger.Application.Handlers.Charges;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;
using Xunit;

namespace RideLedger.Application.Tests.Handlers.Charges;

/// <summary>
/// Unit tests for RecordChargeCommandHandler
/// Tests business logic, error handling, and repository interactions
/// </summary>
public sealed class RecordChargeCommandHandlerTests
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly RecordChargeCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RecordChargeCommandHandlerTests()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _tenantProvider = Substitute.For<ITenantProvider>();
        
        _tenantProvider.GetTenantId().Returns(_tenantId);
        
        _handler = new RecordChargeCommandHandler(
            _accountRepository,
            _unitOfWork,
            _tenantProvider);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRecordChargeAndReturnLedgerEntryId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Test Account",
            AccountType.Organization,
            _tenantId);

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Is<AccountId>(id => id.Value == accountId),
            Arg.Any<CancellationToken>())
            .Returns(account);

        _accountRepository.UpdateAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var command = new RecordChargeCommand
        {
            AccountId = accountId,
            RideId = "ride-123",
            Amount = 150.75m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-456"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty("because a ledger entry ID should be returned");

        await _accountRepository.Received(1).GetByIdWithLedgerEntriesAsync(
            Arg.Is<AccountId>(id => id.Value == accountId),
            Arg.Any<CancellationToken>());

        await _accountRepository.Received(1).UpdateAsync(
            Arg.Is<Account>(a => a.Id.Value == accountId && a.LedgerEntries.Count == 2),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var command = new RecordChargeCommand
        {
            AccountId = accountId,
            RideId = "ride-123",
            Amount = 100.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-456"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("not found"));

        await _accountRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Account>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForInactiveAccount_ShouldReturnFailureResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Test Account",
            AccountType.Organization,
            _tenantId);

        // Deactivate the account
        account.Deactivate("Test deactivation");

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var command = new RecordChargeCommand
        {
            AccountId = accountId,
            RideId = "ride-123",
            Amount = 100.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-456"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("inactive"));

        await _accountRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Account>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithZeroAmount_ShouldReturnFailureResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Test Account",
            AccountType.Organization,
            _tenantId);

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var command = new RecordChargeCommand
        {
            AccountId = accountId,
            RideId = "ride-123",
            Amount = 0m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-456"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("invalid"));

        await _accountRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Account>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryUpdateFails_ShouldReturnFailureResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Test Account",
            AccountType.Organization,
            _tenantId);

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        _accountRepository.UpdateAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Database connection failed"));

        var command = new RecordChargeCommand
        {
            AccountId = accountId,
            RideId = "ride-123",
            Amount = 100.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-456"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Database connection failed"));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCreateTwoLedgerEntries_DebitARAndCreditRevenue()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Test Account",
            AccountType.Organization,
            _tenantId);

        Account? capturedAccount = null;
        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        _accountRepository.UpdateAsync(Arg.Do<Account>(a => capturedAccount = a), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var command = new RecordChargeCommand
        {
            AccountId = accountId,
            RideId = "ride-123",
            Amount = 250.50m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-789"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedAccount.Should().NotBeNull();
        capturedAccount!.LedgerEntries.Should().HaveCount(2, "because double-entry accounting creates two entries");

        var debitEntry = capturedAccount.LedgerEntries.FirstOrDefault(e => e.DebitAmount != null);
        var creditEntry = capturedAccount.LedgerEntries.FirstOrDefault(e => e.CreditAmount != null);

        debitEntry.Should().NotBeNull();
        debitEntry!.LedgerAccount.Should().Be(LedgerAccountType.AccountsReceivable);
        debitEntry.DebitAmount!.Amount.Should().Be(250.50m);
        debitEntry.SourceType.Should().Be(SourceType.Ride);
        debitEntry.SourceReferenceId.Should().Be("ride-123");

        creditEntry.Should().NotBeNull();
        creditEntry!.LedgerAccount.Should().Be(LedgerAccountType.ServiceRevenue);
        creditEntry.CreditAmount!.Amount.Should().Be(250.50m);
        creditEntry.SourceType.Should().Be(SourceType.Ride);
        creditEntry.SourceReferenceId.Should().Be("ride-123");
    }

    [Fact]
    public async Task Handle_WithDuplicateRideId_ShouldReturnFailureResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create(
            AccountId.Create(accountId),
            "Test Account",
            AccountType.Organization,
            _tenantId);

        // Record first charge
        var firstRideId = RideId.Create("ride-123");
        var firstAmount = Money.Create(100.00m);
        account.RecordCharge(
            firstRideId,
            firstAmount,
            DateTime.UtcNow.Date,
            "fleet-456",
            _tenantId.ToString());

        _accountRepository.GetByIdWithLedgerEntriesAsync(
            Arg.Any<AccountId>(),
            Arg.Any<CancellationToken>())
            .Returns(account);

        var command = new RecordChargeCommand
        {
            AccountId = accountId,
            RideId = "ride-123", // Same RideId as above
            Amount = 150.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-456"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("already been recorded"));

        await _accountRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Account>(),
            Arg.Any<CancellationToken>());
    }
}
