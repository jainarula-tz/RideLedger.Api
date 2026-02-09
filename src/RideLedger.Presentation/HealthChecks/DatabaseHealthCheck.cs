using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RideLedger.Presentation.HealthChecks;

/// <summary>
/// Database health check for PostgreSQL connection
/// Verifies database connectivity and responsiveness
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ILogger<DatabaseHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseHealthCheck(
        ILogger<DatabaseHealthCheck> _logger,
        IConfiguration configuration)
    {
        this._logger = _logger;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Database connection string is not configured");
                return HealthCheckResult.Unhealthy(
                    "Database connection string is missing",
                    data: new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow
                    });
            }

            // Use Npgsql to check database connectivity
            await using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5; // 5 second timeout
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result is 1)
            {
                return HealthCheckResult.Healthy(
                    "Database is responsive",
                    data: new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow,
                        ["database"] = connection.Database ?? "unknown",
                        ["server"] = connection.Host ?? "unknown"
                    });
            }

            return HealthCheckResult.Degraded(
                "Database query returned unexpected result",
                data: new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["result"] = result?.ToString() ?? "null"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy(
                "Database is not accessible",
                ex,
                data: new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["error"] = ex.Message
                });
        }
    }
}
