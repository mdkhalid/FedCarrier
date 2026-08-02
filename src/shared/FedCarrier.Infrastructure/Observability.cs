using System.Diagnostics;
using FedCarrier.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace FedCarrier.Infrastructure;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;

    public CorrelationIdMiddleware(RequestDelegate next, string headerName = Constants.CorrelationIdHeader)
    {
        _next = next;
        _headerName = headerName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(_headerName, out var existing) ||
            string.IsNullOrWhiteSpace(existing.ToString()))
        {
            existing = Guid.NewGuid().ToString();
            context.Request.Headers[_headerName] = existing;
        }

        var correlationId = existing.ToString();
        context.Response.Headers[_headerName] = correlationId;
        context.TraceIdentifier = correlationId;

        await _next(context);
    }
}

public class TelemetryOptions
{
    public string ServiceName { get; set; } = "FedCarrier.Service";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public string SeqEndpoint { get; set; } = "http://localhost:5341";
    public bool EnableMetrics { get; set; } = true;
    public bool EnableTracing { get; set; } = true;
}

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("OpenTelemetry").Get<TelemetryOptions>() ?? new TelemetryOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddMeter("FedCarrier.*");
                metrics.AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddSource("FedCarrier.*");
                tracing.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            });

        services.AddHealthChecks()
            .AddCheck("sqlserver", () =>
            {
                var cs = configuration.GetConnectionString("DefaultConnection") ?? "";
                var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cs);
                csb.InitialCatalog = "master";
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
                conn.Open();
                return HealthCheckResult.Healthy("SQL Server reachable");
            });
        return services;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.MapHealthChecks("/health");
        if (app.Configuration.GetSection("OpenTelemetry").Get<TelemetryOptions>()?.EnableMetrics ?? true)
            app.MapPrometheusScrapingEndpoint();
        return app;
    }

    public static void AddDefaultSerilog(WebApplicationBuilder builder)
    {
        var seqEndpoint = builder.Configuration.GetSection("OpenTelemetry").Get<TelemetryOptions>()?.SeqEndpoint
            ?? "http://localhost:5341";

        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration)
                .Enrich.WithProperty("ServiceName", context.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
        });
    }
}
