using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StrongDatabase.Api.Services;
using System.Reflection;

namespace StrongDatabase.Api.Controllers
{
    /// <summary>
    /// Controller for monitoring and health check endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Detailed health check endpoint
        /// </summary>
        /// <returns>Detailed status of all services</returns>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync();
                
                var response = new
                {
                    status = healthReport.Status.ToString().ToLower(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    version = GetApiVersion(),
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    totalDuration = $"{healthReport.TotalDuration.TotalMilliseconds:F2}ms",
                    services = healthReport.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => new
                        {
                            status = entry.Value.Status.ToString().ToLower(),
                            description = entry.Value.Description,
                            duration = $"{entry.Value.Duration.TotalMilliseconds:F2}ms",
                            details = entry.Value.Data
                        }
                    )
                };

                var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing health check");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    error = "Internal health check failure",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Simplified endpoint for quick verification
        /// </summary>
        /// <returns>Simple API status</returns>
        [HttpGet("simple")]
        [ProducesResponseType(200)]
        public IActionResult GetSimpleHealth()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                version = GetApiVersion(),
                message = "API is working correctly"
            });
        }

        /// <summary>
        /// API version information
        /// </summary>
        /// <returns>Version and build information</returns>
        [HttpGet("version")]
        [ProducesResponseType(200)]
        public IActionResult GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var buildDate = GetBuildDate(assembly);

            return Ok(new
            {
                version = GetApiVersion(),
                assemblyVersion = version?.ToString(),
                buildDate = buildDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                framework = Environment.Version.ToString(),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString()
            });
        }

        private static string GetApiVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }

        private static DateTime? GetBuildDate(Assembly assembly)
        {
            try
            {
                var attribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
                if (attribute != null && DateTime.TryParse(attribute.Value, out var buildDate))
                {
                    return buildDate;
                }
                
                // Fallback: use file creation date
                var location = assembly.Location;
                if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
                {
                    return System.IO.File.GetCreationTimeUtc(location);
                }
            }
            catch
            {
                // Ignore errors and return null
            }
            
            return null;
        }
    }
} 