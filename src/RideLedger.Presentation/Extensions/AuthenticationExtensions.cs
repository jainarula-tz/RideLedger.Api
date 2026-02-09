using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace RideLedger.Presentation.Extensions;

/// <summary>
/// JWT authentication configuration extensions
/// Senior developer pattern: Centralize auth configuration for maintainability
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures JWT Bearer authentication with RSA or HMAC validation
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("JwtSettings");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        var secretKey = jwtSection["SecretKey"]; // For development only - use RSA in production

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = true; // Enforce HTTPS in production
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = string.IsNullOrEmpty(secretKey)
                    ? null // Use RSA key from configuration
                    : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 min clock skew
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc9457",
                        title = "Unauthorized",
                        status = 401,
                        detail = "Valid JWT token is required",
                        instance = context.Request.Path.Value
                    });
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Configures authorization policies for role, tenant access
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Admin policy
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin", "SuperAdmin"));

            // Billing administrator policy
            options.AddPolicy("BillingAdmin", policy =>
                policy.RequireRole("Admin", "BillingAdmin"));

            // Tenant access policy (requires tenant_id claim)
            options.AddPolicy("TenantAccess", policy =>
                policy.RequireClaim("tenant_id"));
        });

        return services;
    }
}
