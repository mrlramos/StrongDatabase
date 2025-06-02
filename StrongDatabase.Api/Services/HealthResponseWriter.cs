using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace StrongDatabase.Api.Services
{
    /// <summary>
    /// Class responsible for formatting health check responses
    /// </summary>
    public static class HealthResponseWriter
    {
        /// <summary>
        /// Writes the health check response in detailed JSON format
        /// </summary>
        public static async Task WriteResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var healthReport = new
            {
                status = report.Status.ToString().ToLower(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                results = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString().ToLower(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration.TotalMilliseconds,
                        data = entry.Value.Data
                    }
                )
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(healthReport, options));
        }
    }
} 