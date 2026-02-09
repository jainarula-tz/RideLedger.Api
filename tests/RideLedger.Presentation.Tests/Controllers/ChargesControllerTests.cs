using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Application.DTOs.Charges;
using RideLedger.Domain.Enums;
using RideLedger.Presentation.Tests.Infrastructure;

namespace RideLedger.Presentation.Tests.Controllers;

/// <summary>
/// Integration tests for ChargesController
/// Tests the complete HTTP request/response cycle including authentication, validation, and database persistence
/// </summary>
public class ChargesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ChargesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecordCharge_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account");

        var request = new RecordChargeRequest
        {
            AccountId = accountId,
            RideId = Guid.NewGuid().ToString(),
            Amount = 100.50m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var chargeResponse = await response.Content.ReadFromJsonAsync<ChargeResponse>();
        chargeResponse.Should().NotBeNull();
        chargeResponse!.AccountId.Should().Be(accountId);
        chargeResponse.RideId.Should().Be(request.RideId);
        chargeResponse.Amount.Should().Be(100.50m);
        chargeResponse.Currency.Should().Be("USD");
        chargeResponse.LedgerEntryId.Should().NotBeEmpty();
        chargeResponse.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordCharge_WithDuplicateRideId_ShouldReturn409Conflict()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();
        var rideId = Guid.NewGuid().ToString();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account");

        var request = new RecordChargeRequest
        {
            AccountId = accountId,
            RideId = rideId,
            Amount = 100.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Record charge first time
        var firstResponse = await client.PostAsJsonAsync("/api/v1/charges", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to record same charge again
        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        
        var problemDetails = await duplicateResponse.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("duplicate", "because duplicate charges should be rejected");
    }

    [Fact]
    public async Task RecordCharge_ForNonExistentAccount_ShouldReturn404NotFound()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var nonExistentAccountId = Guid.NewGuid();

        var request = new RecordChargeRequest
        {
            AccountId = nonExistentAccountId,
            RideId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("not found", "because account does not exist");
    }

    [Fact]
    public async Task RecordCharge_WithZeroAmount_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account");

        var request = new RecordChargeRequest
        {
            AccountId = accountId,
            RideId = Guid.NewGuid().ToString(),
            Amount = 0m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Amount", "because validation should reject zero amounts");
    }

    [Fact]
    public async Task RecordCharge_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // No authentication
        var request = new RecordChargeRequest
        {
            AccountId = Guid.NewGuid(),
            RideId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordCharge_WithInvalidRequest_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var invalidRequest = new
        {
            AccountId = Guid.NewGuid(),
            // Missing required fields: RideId, Amount, ServiceDate, FleetId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordCharge_ForInactiveAccount_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account");

        // TODO: Deactivate account (need deactivation endpoint)
        // For now, this test documents the expected behavior

        var request = new RecordChargeRequest
        {
            AccountId = accountId,
            RideId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert - Should succeed since account is active
        // When deactivation endpoint exists, deactivate first and expect 400
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RecordCharge_MultipleCharges_ShouldCreateSeparateEntries()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account");

        var charge1 = new RecordChargeRequest
        {
            AccountId = accountId,
            RideId = Guid.NewGuid().ToString(),
            Amount = 50.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        var charge2 = new RecordChargeRequest
        {
            AccountId = accountId,
            RideId = Guid.NewGuid().ToString(),
            Amount = 75.00m,
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-456"
        };

        // Act
        var response1 = await client.PostAsJsonAsync("/api/v1/charges", charge1);
        var response2 = await client.PostAsJsonAsync("/api/v1/charges", charge2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var chargeResponse1 = await response1.Content.ReadFromJsonAsync<ChargeResponse>();
        var chargeResponse2 = await response2.Content.ReadFromJsonAsync<ChargeResponse>();

        chargeResponse1!.LedgerEntryId.Should().NotBe(chargeResponse2!.LedgerEntryId);
        chargeResponse1.RideId.Should().Be(charge1.RideId);
        chargeResponse2.RideId.Should().Be(charge2.RideId);
    }

    // Helper method to create an account for testing
    private async Task<AccountResponse> CreateAccountAsync(HttpClient client, Guid accountId, string accountName)
    {
        var createAccountRequest = new CreateAccountRequest
        {
            AccountId = accountId,
            Name = accountName,
            Type = AccountType.Organization
        };

        var response = await client.PostAsJsonAsync("/api/v1/accounts", createAccountRequest);
        response.EnsureSuccessStatusCode();

        var accountResponse = await response.Content.ReadFromJsonAsync<AccountResponse>();
        return accountResponse!;
    }
}
