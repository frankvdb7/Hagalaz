using Aspire.Hosting.Lifecycle;


namespace Hagalaz.AppHost.Telemetry
{
    public static class OpenTelemetryCollectorServiceExtensions
    {
        public static IDistributedApplicationBuilder AddOpenTelemetryCollectorInfrastructure(this IDistributedApplicationBuilder builder)
        {
            builder.Services.TryAddLifecycleHook<OpenTelemetryCollectorLifecycleHook>();

            return builder;
        }
    }
}