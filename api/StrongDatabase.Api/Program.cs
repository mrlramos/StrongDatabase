using System.Text.Json;
using System.Text.Json.Serialization;
using StrongDatabase.Api.Data;
using StrongDatabase.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Enrichers.CorrelationId;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("🚀 [StrongDatabase] Iniciando aplicação...");

    var builder = WebApplication.CreateBuilder(args);

    // Configurar Serilog
    builder.Host.UseSerilog();

    // Adicionar CorrelationId enricher
    builder.Services.AddHttpContextAccessor();

    // Database routing services - no default DbContext registration
    builder.Services.AddSingleton<DbContextRouter>();
    builder.Services.AddScoped<Repository>();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Custom Health Check configuration
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheckService>("database_health_check");

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Adicionar middleware de Request Logging do Serilog
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.IncludeQueryInRequestPath = true;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            
            if (httpContext.Request.ContentLength.HasValue)
                diagnosticContext.Set("RequestLength", httpContext.Request.ContentLength);
                
            if (httpContext.Response.ContentLength.HasValue)
                diagnosticContext.Set("ResponseLength", httpContext.Response.ContentLength);
        };
    });

    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint configuration with detailed JSON response
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthResponseWriter.WriteResponse
    });

    Log.Information("✅ [StrongDatabase] Aplicação configurada! Listening on {Ports}", 
        string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 [StrongDatabase] Falha crítica na inicialização da aplicação");
}
finally
{
    Log.Information("🛑 [StrongDatabase] Aplicação finalizando...");
    Log.CloseAndFlush();
}