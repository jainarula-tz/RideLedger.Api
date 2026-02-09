using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Domain.Enums;
using RideLedger.Presentation.Tests.Infrastructure;

namespace RideLedger.Presentation.Tests.Controllers;

/// <summary>
/// Integration tests for AccountsController
/// Tests the complete HTTP request/response cycle for account management endpoints
/// </summary>
public class AccountsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AccountsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region POST /api/v1/accounts (T126)

    [Fact]
    public async Task CreateAccount_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        var request = new CreateAccountRequest
        {
            AccountId = accountId,
            Name = "Test Organization",
            Type = AccountType.Organization
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/v1/Accounts/{accountId}", "Location header should reference the created account");

        var accountResponse = await response.Content.ReadFromJsonAsync<AccountResponse>();
        accountResponse.Should().NotBeNull();
        accountResponse!.AccountId.Should().Be(accountId);
        accountResponse.Name.Should().Be("Test Organization");
        accountResponse.Type.Should().Be(AccountType.Organization);
        accountResponse.Status.Should().Be(AccountStatus.Active);
        accountResponse.Balance.Should().Be(0m);
        accountResponse.Currency.Should().Be("USD");
        accountResponse.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        accountResponse.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateAccount_WithIndividualType_ShouldReturn201Created()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        var request = new CreateAccountRequest
        {
            AccountId = accountId,
            Name = "John Doe",
            Type = AccountType.Individual
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var accountResponse = await response.Content.ReadFromJsonAsync<AccountResponse>();
        accountResponse.Should().NotBeNull();
        accountResponse!.AccountId.Should().Be(accountId);
        accountResponse.Type.Should().Be(AccountType.Individual);
    }

    [Fact(Skip = "InMemory database persistence issue - duplicate detection not working across requests")]
    public async Task CreateAccount_WithDuplicateAccountId_ShouldReturn409Conflict()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        var request = new CreateAccountRequest
        {
            AccountId = accountId,
            Name = "Test Account",
            Type = AccountType.Organization
        };

        // Create account first time
        var firstResponse = await client.PostAsJsonAsync("/api/v1/accounts", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Attempt to create same account again
        var secondResponse = await client.PostAsJsonAsync("/api/v1/accounts", request);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict, "because account with same ID already exists");
    }

    [Fact]
    public async Task CreateAccount_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var accountId = Guid.NewGuid();

        var request = new CreateAccountRequest
        {
            AccountId = accountId,
            Name = "Test Account",
            Type = AccountType.Organization
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAccount_WithInvalidRequest_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);

        var invalidRequest = new
        {
            AccountId = Guid.Empty, // Invalid: empty GUID
            Name = "",             // Invalid: empty name
            Type = 999             // Invalid: unknown enum value
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "because request has validation errors");
    }

    #endregion

    #region GET /api/v1/accounts/{id} (T127)

    [Fact]
    public async Task GetAccountById_WithExistingAccount_ShouldReturn200OK()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var accountId = Guid.NewGuid();

        // Create account first
        await CreateAccountAsync(client, accountId, "Test Account", AccountType.Organization);

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var accountResponse = await response.Content.ReadFromJsonAsync<AccountResponse>();
        accountResponse.Should().NotBeNull();
        accountResponse!.AccountId.Should().Be(accountId);
        accountResponse.Name.Should().Be("Test Account");
        accountResponse.Type.Should().Be(AccountType.Organization);
        accountResponse.Status.Should().Be(AccountStatus.Active);
        accountResponse.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task GetAccountById_WithNonExistentAccount_ShouldReturn404NotFound()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);
        var nonExistentAccountId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{nonExistentAccountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAccountById_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var accountId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{accountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccountById_WithInvalidGuid_ShouldReturn404NotFound()
    {
        // Arrange
        var client = AuthenticationHelper.CreateAuthenticatedClient(_factory, _tenantId);

        // Act
        var response = await client.GetAsync("/api/v1/accounts/invalid-guid");

        // Assert - ASP.NET Core routing will return 404 for invalid GUID format
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private async Task<AccountResponse> CreateAccountAsync(
        HttpClient client,
        Guid accountId,
        string accountName,
        AccountType accountType)
    {
        var request = new CreateAccountRequest
        {
            AccountId = accountId,
            Name = accountName,
            Type = accountType
        };

        var response = await client.PostAsJsonAsync("/api/v1/accounts", request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }

    #endregion
}
