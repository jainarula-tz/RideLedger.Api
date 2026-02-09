using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Application.DTOs.Charges;
using RideLedger.Application.DTOs.Payments;
using RideLedger.Domain.Enums;
using RideLedger.Presentation.Tests.Infrastructure;

namespace RideLedger.Presentation.Tests.EdgeCases;

/// <summary>
/// Edge case integration tests for validation scenarios
/// Verifies that invalid inputs are properly rejected as per spec.md edge cases
/// </summary>
public class ValidationEdgeCaseTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ValidationEdgeCaseTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ========================================
    // T156: Zero-amount charge rejection
    // ========================================
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
            Amount = 0.00m, // Zero amount - should be rejected
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because zero-amount charges violate FR-021 (positive amount requirement)");

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().ContainAny("amount", "Amount");
    }

    // ========================================
    // T157: Negative-amount charge rejection
    // ========================================
    [Fact]
    public async Task RecordCharge_WithNegativeAmount_ShouldReturn400BadRequest()
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
            Amount = -50.00m, // Negative amount - should be rejected
            ServiceDate = DateTime.UtcNow.Date,
            FleetId = "fleet-123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/charges", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because negative-amount charges violate FR-021 (positive amount requirement)");

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().ContainAny("amount", "Amount");
    }

    // ========================================
    // T158: Zero-amount payment rejection
    // ========================================
    [Fact]
    public async Task RecordPayment_WithZeroAmount_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account");

        var request = new RecordPaymentRequest
        {
            AccountId = accountId,
            PaymentReferenceId = Guid.NewGuid().ToString(),
            Amount = 0.00m, // Zero amount - should be rejected
            PaymentDate = DateTime.UtcNow.Date,
            PaymentMode = PaymentMode.Cash
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because zero-amount payments violate FR-021 (positive amount requirement)");

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().ContainAny("amount", "Amount");
    }

    // ========================================
    // T159: Negative-amount payment rejection
    // ========================================
    [Fact]
    public async Task RecordPayment_WithNegativeAmount_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account");

        var request = new RecordPaymentRequest
        {
            AccountId = accountId,
            PaymentReferenceId = Guid.NewGuid().ToString(),
            Amount = -100.00m, // Negative amount - should be rejected
            PaymentDate = DateTime.UtcNow.Date,
            PaymentMode = PaymentMode.Card
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because negative-amount payments violate FR-021 (positive amount requirement)");

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().ContainAny("amount", "Amount");
    }

    // ========================================
    // T160: Charge to inactive account rejection
    // ========================================
    [Fact(Skip = "Account deactivation functionality not yet implemented")]
    public async Task RecordCharge_ToInactiveAccount_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account
        await CreateAccountAsync(client, accountId, "Test Account");

        // TODO: Deactivate account (needs deactivation endpoint)
        // await client.PostAsync($"/api/v1/accounts/{accountId}/deactivate", null);

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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because charges to inactive accounts violate FR-022");

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().ContainAny("inactive", "Inactive");
    }

    // ========================================
    // T161: Payment to inactive account rejection
    // ========================================
    [Fact(Skip = "Account deactivation functionality not yet implemented")]
    public async Task RecordPayment_ToInactiveAccount_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account
        await CreateAccountAsync(client, accountId, "Test Account");

        // TODO: Deactivate account (needs deactivation endpoint)
        // await client.PostAsync($"/api/v1/accounts/{accountId}/deactivate", null);

        var request = new RecordPaymentRequest
        {
            AccountId = accountId,
            PaymentReferenceId = Guid.NewGuid().ToString(),
            Amount = 50.00m,
            PaymentDate = DateTime.UtcNow.Date,
            PaymentMode = PaymentMode.Cash
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because payments to inactive accounts violate FR-022");

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().ContainAny("inactive", "Inactive");
    }

    // ========================================
    // T162: Large transaction volume handling
    // ========================================
    [Fact(Skip = "Performance test - requires optimization and proper timing infrastructure")]
    public async Task GetBalance_WithLargeTransactionVolume_ShouldCompleteInUnder50ms()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        await CreateAccountAsync(client, accountId, "High Volume Account");

        // Record 10,000 ledger entries (5,000 charges)
        for (int i = 0; i < 5000; i++)
        {
            var charge = new RecordChargeRequest
            {
                AccountId = accountId,
                RideId = Guid.NewGuid().ToString(),
                Amount = 10.00m + (i % 100), // Varying amounts
                ServiceDate = DateTime.UtcNow.AddDays(-i).Date,
                FleetId = $"fleet-{i % 10}"
            };
            await client.PostAsJsonAsync("/api/v1/charges", charge);
        }

        // Act - Measure balance query performance
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.GetAsync($"/api/v1/accounts/{accountId}/balance");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
            "because balance queries must complete in <50ms p95 per SC-002");
    }

    // ========================================
    // T155: Concurrent charge recording
    // ========================================
    [Fact(Skip = "Concurrency test - requires proper transaction isolation testing")]
    public async Task RecordCharge_ConcurrentRequests_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        await CreateAccountAsync(client, accountId, "Concurrent Test Account");

        // Act - Record 10 charges concurrently
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            var request = new RecordChargeRequest
            {
                AccountId = accountId,
                RideId = Guid.NewGuid().ToString(),
                Amount = 100.00m,
                ServiceDate = DateTime.UtcNow.Date,
                FleetId = "fleet-123"
            };
            tasks.Add(client.PostAsJsonAsync("/api/v1/charges", request));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.Created));

        // Verify balance is correct (10 charges × $100 = $1000)
        var balanceResponse = await client.GetAsync($"/api/v1/accounts/{accountId}/balance");
        balanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await balanceResponse.Content.ReadAsStringAsync();
        content.Should().MatchRegex(@"1000(\.00)?");
    }

    // Helper method to create account
    private async Task CreateAccountAsync(HttpClient client, Guid accountId, string name)
    {
        var createAccountRequest = new CreateAccountRequest
        {
            AccountId = accountId,
            Name = name,
            Type = AccountType.Organization
        };

        var response = await client.PostAsJsonAsync("/api/v1/accounts", createAccountRequest);
        response.EnsureSuccessStatusCode();
    }
}
