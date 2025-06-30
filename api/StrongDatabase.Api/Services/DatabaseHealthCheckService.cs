using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System.Diagnostics;

namespace StrongDatabase.Api.Services
{
    /// <summary>
    /// Custom health check service that verifies each database server individually
    /// </summary>
    public class DatabaseHealthCheckService : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseHealthCheckService> _logger;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public DatabaseHealthCheckService(IConfiguration configuration, ILogger<DatabaseHealthCheckService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthData = new Dictionary<string, object>();
            var overallHealthy = true;
            var stopwatch = Stopwatch.StartNew();

            // API information
            var uptime = DateTime.UtcNow - _startTime;
            healthData["api"] = new
            {
                status = "healthy",
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
                start_time = _startTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            // Check each database server
            var databases = new Dictionary<string, string>
            {
                ["primary"] = _configuration.GetConnectionString("DefaultConnection")!,
                ["standby"] = _configuration.GetConnectionString("Standby")!,
                ["replica1"] = _configuration.GetConnectionString("Replica1")!,
                ["replica2"] = _configuration.GetConnectionString("Replica2")!
            };

            foreach (var (name, connectionString) in databases)
            {
                var dbHealth = await CheckDatabaseHealthAsync(name, connectionString, cancellationToken);
                healthData[name] = dbHealth;
                
                if (dbHealth.GetType().GetProperty("status")?.GetValue(dbHealth)?.ToString() != "healthy")
                {
                    overallHealthy = false;
                }
            }

            stopwatch.Stop();
            healthData["total_check_duration_ms"] = stopwatch.ElapsedMilliseconds;

            var status = overallHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;
            var description = overallHealthy 
                ? "All services are working correctly" 
                : "Some services have issues";

            return new HealthCheckResult(status, description, data: healthData);
        }

        private async Task<object> CheckDatabaseHealthAsync(string name, string connectionString, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                
                // Check if we can execute a simple query
                using var command = new NpgsqlCommand(@"
                    SELECT 
                        version(), 
                        current_database(), 
                        current_user,
                        COALESCE(inet_server_addr()::text, 'localhost') as server_addr,
                        COALESCE(inet_server_port(), 5432) as server_port;", connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                
                if (await reader.ReadAsync(cancellationToken))
                {
                    var version = reader.GetString(0);
                    var database = reader.GetString(1);
                    var user = reader.GetString(2);
                    var serverAddr = reader.GetString(3);
                    var serverPort = reader.GetInt32(4);

                    stopwatch.Stop();

                    return new
                    {
                        status = "healthy",
                        response_time_ms = stopwatch.ElapsedMilliseconds,
                        database_name = database,
                        user = user,
                        server_address = serverAddr,
                        server_port = serverPort,
                        postgresql_version = version.Split(' ').Length > 1 ? version.Split(' ')[1] : version,
                        last_check = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    };
                }
                else
                {
                    stopwatch.Stop();
                    return new
                    {
                        status = "unhealthy",
                        response_time_ms = stopwatch.ElapsedMilliseconds,
                        error = "Unable to execute verification query",
                        last_check = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    };
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Failed to check health of database {DatabaseName}", name);
                
                return new
                {
                    status = "unhealthy",
                    response_time_ms = stopwatch.ElapsedMilliseconds,
                    error = ex.Message,
                    error_type = ex.GetType().Name,
                    last_check = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
            }
        }
    }
} 