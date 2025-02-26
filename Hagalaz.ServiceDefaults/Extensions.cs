using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Scalar.AspNetCore;

namespace Hagalaz.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddCors();

        // TODO - API / controllers not binding
        builder.Services
            .AddControllers()
            .AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        builder.Services.AddAuthorization();

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Configuration.AddEnvironmentVariables(EnvironmentVariables.Prefix);

        builder.Services.AddResiliencePipelineRegistry<string>();

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        builder.Services.AddHttpForwarderWithServiceDiscovery();

        builder.Services.AddOpenApi(options => { options.AddDocumentTransformer<OpenApi.OpenIdConnectSecuritySchemeTransformer>(); });

        // allow a client to call you without specifying an api version
        // since we haven't configured it otherwise, the assumed api version will be v1
        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddHagalazMeters();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // We want to view all traces in development
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("MassTransit")
                    .AddSource("Polly")
                    .AddSource("Raido.Server");
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRequestTimeouts(configure: static timeouts =>
            timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));

        builder.Services.AddOutputCache(configureOptions: static caching =>
            caching.AddPolicy("HealthChecks",
                build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        var healthChecks = app.MapGroup("");

        healthChecks
            .CacheOutput("HealthChecks")
            .WithRequestTimeout("HealthChecks");

        // All health checks must pass for app to be considered ready to accept traffic after starting
        healthChecks.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        healthChecks.MapHealthChecks("/alive",
            new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });

        return app;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseRouting();
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseForwardedHeaders();
            app.UseCors(builder => { builder.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseForwardedHeaders();
            app.UseHsts();
            app.UseCors();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapDefaultEndpoints();
        app.MapControllers();
        return app;
    }

    public static async Task MigrateDatabase<TContext>(this WebApplication app, Func<CancellationToken, Task>? initializationTask = null)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                BackoffType = DelayBackoffType.Exponential, Delay = TimeSpan.FromSeconds(5), MaxDelay = TimeSpan.FromMinutes(5)
            })
            .Build();
        try
        {
            await pipeline.ExecuteAsync(async token =>
            {
                await context.Database.MigrateAsync(token);
                if (initializationTask != null)
                {
                    await initializationTask(token);
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to perform database migration");
        }
    }

    private static MeterProviderBuilder AddHagalazMeters(this MeterProviderBuilder meterProviderBuilder) =>
        meterProviderBuilder.AddMeter("System.Net.Http",
            "MassTransit",
            "Polly",
            "Raido.Server");

    public static string? GetServiceConfigurationValue(this IConfiguration configuration, string serviceName, string key, string? fallbackKey = null)
    {
        var val = configuration.GetValue<string>($"services:{serviceName}:{key}:0");
        return val ?? configuration.GetValue<string>($"services:{serviceName}:{fallbackKey}:0");
    }
}