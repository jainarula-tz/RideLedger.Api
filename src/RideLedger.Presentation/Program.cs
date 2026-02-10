using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RideLedger.Application;
using RideLedger.Infrastructure;
using RideLedger.Presentation.Extensions;
using RideLedger.Presentation.Filters;
using RideLedger.Presentation.Middleware;
using Serilog;

// ============================================================================
// SERILOG CONFIGURATION - Structured Logging
// ============================================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/rideledger-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

try
{
    Log.Information("Starting RideLedger.Presentation application");

    var builder = WebApplication.CreateBuilder(args);

    // ========================================================================
    // LOGGING - Serilog Integration
    // ========================================================================
    builder.Host.UseSerilog();

    // ========================================================================
    // SERVICE CONFIGURATION - Clean Architecture Pattern
    // ========================================================================

    // Add HTTP Context Accessor for accessing HttpContext in services
    builder.Services.AddHttpContextAccessor();

    // ========================================================================
    // LAYER REGISTRATION - Onion Architecture Pattern
    // ========================================================================
    
    // Application Layer (Business Logic, Commands, Queries, Validators)
    builder.Services.AddApplication();
    
    // Infrastructure Layer (DbContext, Repositories, External Services)
    builder.Services.AddInfrastructure(builder.Configuration);

    // JWT Authentication & Authorization (Senior Dev Pattern: Secure by default)
    // TODO: Re-enable after implementing authentication
    // builder.Services.AddJwtAuthentication(builder.Configuration);
    // builder.Services.AddAuthorizationPolicies();

    // Controllers with Filters and JSON configuration
    builder.Services.AddControllers(options =>
    {
        // Order matters! Filters execute in the order they're added
        // TODO: Re-enable after implementing authentication
        // options.Filters.Add<TenantAuthorizationFilter>(); // 1. Check auth & tenant
        options.Filters.Add<ValidationFilter>();           // 2. Validate request
        options.Filters.Add<PerformanceMonitoringFilter>(); // 3. Monitor performance
    })
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization to use camelCase for property names
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Allow trailing commas in JSON (more lenient)
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        // Ignore null values when writing JSON (cleaner responses)
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    // API Documentation - Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "RideLedger Accounting API",
            Version = "v1",
            Description = "Dual-entry accounting and invoicing service for ride services",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "RideLedger Team",
                Email = "support@rideledger.com"
            }
        });

        // JWT Bearer authentication in Swagger
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Health Checks - Database health check is registered in Infrastructure layer
    // Additional custom health checks can be added here if needed

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? Array.Empty<string>())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ========================================================================
    // APPLICATION BUILD
    // ========================================================================
    var app = builder.Build();

    // ========================================================================
    // MIDDLEWARE PIPELINE - Order is Critical!
    // ========================================================================
    // Senior Developer Note: Middleware executes top-to-bottom for requests,
    // bottom-to-top for responses

    // 1. HTTPS Redirection (Security)
    if (app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    // 2. Swagger (Development Only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "RideLedger API v1");
            options.RoutePrefix = string.Empty; // Serve at root
        });
    }

    // 3. Request Logging Middleware (Log all requests)
    app.UseMiddleware<RequestLoggingMiddleware>();

    // 4. CORS (Must be before authentication)
    app.UseCors("AllowFrontend");

    // 5. Authentication & Authorization (JWT validation)
    // TODO: Re-enable after implementing authentication
    // app.UseAuthentication();
    // app.UseAuthorization();

    // 6. Global Exception Handler (Catch all unhandled exceptions)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // 7. Map Controllers
    app.MapControllers();

    // 8. Health Check Endpoints
    // Liveness: Is the app running? (simple, no external dependencies)
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false, // No checks, just return 200 OK if app is running
        AllowCachingResponses = false
    }).AllowAnonymous();
    
    // Readiness: Can the app handle requests? (checks DB, external dependencies)
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        AllowCachingResponses = false,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    data = e.Value.Data
                })
            });
            await context.Response.WriteAsync(result);
        }
    }).AllowAnonymous();
    
    // Startup: Has the app finished starting? (checks critical startup dependencies)
    app.MapHealthChecks("/health/startup", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("startup"),
        AllowCachingResponses = false
    }).AllowAnonymous();

    // ========================================================================
    // START APPLICATION
    // ========================================================================
    Log.Information("Application configured successfully. Starting web host...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }

