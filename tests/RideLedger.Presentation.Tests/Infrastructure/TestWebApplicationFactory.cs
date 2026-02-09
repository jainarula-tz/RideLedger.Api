using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using RideLedger.Infrastructure.Persistence;

namespace RideLedger.Presentation.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// Configures in-memory database and test-specific services
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestSecretKey = "TestSecretKeyForJwtTokenGeneration_MustBe32Chars!!!";
    private const string TestIssuer = "RideLedger.Test";
    private const string TestAudience = "RideLedger.API.Test";
    
    // Unique database name per factory instance so tests can share data within the same test
    private readonly string _databaseName = $"TestDatabase_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test environment
        builder.UseEnvironment("Testing");

        // Configure to use in-memory database for tests
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.UseSetting("InMemoryDatabaseName", _databaseName);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "InMemory");

        builder.ConfigureTestServices(services =>
        {
            // Ensure the database is created
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
                db.Database.EnsureCreated();
            }
            
            // Configure JWT authentication with test settings
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        });
    }
}
