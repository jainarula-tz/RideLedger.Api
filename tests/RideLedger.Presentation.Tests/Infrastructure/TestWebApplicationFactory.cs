using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test environment
        builder.UseEnvironment("Testing");

        // Add test configuration settings BEFORE host is built
        builder.UseSetting("ConnectionStrings:DefaultConnection", 
            "Host=localhost;Database=testdb;Username=test;Password=test");

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext configuration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AccountingDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<AccountingDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
            });

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

            // Build the service provider and create database
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AccountingDbContext>();

            // Ensure database is created
            db.Database.EnsureCreated();
        });
    }
}
