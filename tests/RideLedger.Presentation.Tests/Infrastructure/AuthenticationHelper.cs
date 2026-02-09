using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RideLedger.Presentation.Tests.Infrastructure;

/// <summary>
/// Helper class for creating authenticated HTTP clients with JWT tokens for testing
/// </summary>
public static class AuthenticationHelper
{
    private const string TestSecretKey = "TestSecretKeyForJwtTokenGeneration_MustBe32Chars!!!";
    private const string TestIssuer = "RideLedger.Test";
    private const string TestAudience = "RideLedger.API.Test";

    /// <summary>
    /// Creates an authenticated HTTP client with a JWT token containing the specified claims
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(
        TestWebApplicationFactory factory,
        Guid tenantId,
        string userId = "test-user",
        string role = "User")
    {
        var client = factory.CreateClient();
        var token = GenerateJwtToken(tenantId, userId, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Generates a JWT token for testing with tenant_id claim
    /// </summary>
    public static string GenerateJwtToken(Guid tenantId, string userId = "test-user", string role = "User")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("tenant_id", tenantId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
