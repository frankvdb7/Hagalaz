using System.Net;
using System.Net.Sockets;
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
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Scalar.AspNetCore;
using ForwardedHeadersNetwork = System.Net.IPNetwork;

namespace Hagalaz.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        return builder.AddServiceDefaults(requireTrustedForwardedHeaders: false);
    }

    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder,
        bool requireTrustedForwardedHeaders)
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

        builder.Configuration.AddEnvironmentVariables(EnvironmentVariables.Prefix);
        builder.AddForwardedHeaders(requireTrustedForwardedHeaders);

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

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

    public static IHostApplicationBuilder AddForwardedHeaders(
        this IHostApplicationBuilder builder,
        bool requireTrustedForwardedHeaders = false)
    {
        var knownProxies = ParseKnownProxies(builder.Configuration);
        var knownNetworks = ParseKnownNetworks(builder.Configuration);
        var hasExplicitTrust = knownProxies.Count > 0 || knownNetworks.Count > 0;

        if (requireTrustedForwardedHeaders && !hasExplicitTrust)
        {
            throw new InvalidOperationException(
                "Forwarded headers are required outside Development, but no trusted proxy or network is configured. " +
                "Configure ForwardedHeaders:KnownProxies or ForwardedHeaders:KnownNetworks.");
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            if (!hasExplicitTrust)
            {
                return;
            }

            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(proxy);
            }

            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });

        return builder;
    }

    private static List<IPAddress> ParseKnownProxies(IConfiguration configuration)
    {
        var proxies = new List<IPAddress>();
        foreach (var child in configuration.GetSection("ForwardedHeaders:KnownProxies").GetChildren())
        {
            if (string.IsNullOrWhiteSpace(child.Value) || !IPAddress.TryParse(child.Value, out var address))
            {
                throw new InvalidOperationException(
                    $"The forwarded-header trusted proxy '{child.Value}' is not a valid IP address.");
            }

            proxies.Add(address);
        }

        return proxies;
    }

    private static List<ForwardedHeadersNetwork> ParseKnownNetworks(IConfiguration configuration)
    {
        var networks = new List<ForwardedHeadersNetwork>();
        foreach (var child in configuration.GetSection("ForwardedHeaders:KnownNetworks").GetChildren())
        {
            var value = child.Value;
            var parts = value?.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts is not { Length: 2 } ||
                !IPAddress.TryParse(parts[0], out var prefix) ||
                !int.TryParse(parts[1], out var prefixLength))
            {
                throw new InvalidOperationException(
                    $"The forwarded-header trusted network '{value}' is not a valid CIDR network.");
            }

            var maximumPrefixLength = prefix.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            if (prefixLength < 0 || prefixLength > maximumPrefixLength)
            {
                throw new InvalidOperationException(
                    $"The forwarded-header trusted network '{value}' has an invalid prefix length.");
            }

            networks.Add(new ForwardedHeadersNetwork(prefix, prefixLength));
        }

        return networks;
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
                    .AddEntityFrameworkCoreInstrumentation()
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
        app.UseForwardedHeaders();
        app.UseRouting();
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseCors(builder => { builder.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
            app.UseCors();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapDefaultEndpoints();
        app.MapControllers();
        return app;
    }

    public static async Task MigrateDatabase<TContext>(
        this IHost app,
        Func<CancellationToken, Task>? initializationTask = null,
        CancellationToken cancellationToken = default)
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
                await context.Database.OpenConnectionAsync(token);
                var connection = context.Database.GetDbConnection();
                await using var lockCommand = connection.CreateCommand();
                lockCommand.CommandText = "SELECT GET_LOCK('hagalaz-schema-migration', 120);";
                lockCommand.CommandTimeout = 125;
                var lockResult = await lockCommand.ExecuteScalarAsync(token);
                if (Convert.ToInt32(lockResult) != 1)
                {
                    throw new InvalidOperationException("Could not acquire the database migration lock within 120 seconds.");
                }

                try
                {
                    await context.Database.MigrateAsync(token);
                    if (initializationTask != null)
                    {
                        await initializationTask(token);
                    }
                }
                finally
                {
                    await using var releaseCommand = connection.CreateCommand();
                    releaseCommand.CommandText = "SELECT RELEASE_LOCK('hagalaz-schema-migration');";
                    await releaseCommand.ExecuteScalarAsync(token);
                    await context.Database.CloseConnectionAsync();
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to perform database migration");
            throw;
        }
    }

    private static MeterProviderBuilder AddHagalazMeters(this MeterProviderBuilder meterProviderBuilder) =>
        meterProviderBuilder.AddMeter("System.Net.Http",
            "MassTransit",
            "Polly",
            "Raido.Server",
            "Hagalaz.Services.Characters.Persistence");

    public static string? GetServiceConfigurationValue(this IConfiguration configuration, string serviceName, string key, string? fallbackKey = null)
    {
        var val = configuration.GetValue<string>($"services:{serviceName}:{key}:0");
        return val ?? configuration.GetValue<string>($"services:{serviceName}:{fallbackKey}:0");
    }
}
