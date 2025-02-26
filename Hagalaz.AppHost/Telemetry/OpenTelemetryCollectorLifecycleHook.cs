﻿using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Hagalaz.AppHost.Telemetry
{
    public class OpenTelemetryCollectorLifecycleHook : IDistributedApplicationLifecycleHook
    {
        private const string _otelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

        private readonly ILogger<OpenTelemetryCollectorLifecycleHook> _logger;

        public OpenTelemetryCollectorLifecycleHook(ILogger<OpenTelemetryCollectorLifecycleHook> logger)
        {
            _logger = logger;
        }

        public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            var collectorResource = appModel.Resources.OfType<OpenTelemetryCollectorResource>().FirstOrDefault();
            if (collectorResource == null)
            {
                _logger.LogWarning($"No {nameof(OpenTelemetryCollectorResource)} resource found.");
                return Task.CompletedTask;
            }

            var endpoint = collectorResource.GetEndpoint(OpenTelemetryCollectorResource.OtlpGrpcEndpointName);
            if (!endpoint.Exists)
            {
                _logger.LogWarning($"No {OpenTelemetryCollectorResource.OtlpGrpcEndpointName} endpoint for the collector.");
                return Task.CompletedTask;
            }

            foreach (var resource in appModel.Resources)
            {
                resource.Annotations.Add(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
                {
                    if (context.EnvironmentVariables.ContainsKey(_otelExporterOtlpEndpoint))
                    {
                        _logger.LogDebug("Forwarding telemetry for {ResourceName} to the collector.", resource.Name);

                        context.EnvironmentVariables[_otelExporterOtlpEndpoint] = endpoint;
                    }
                }));
            }

            return Task.CompletedTask;
        }
    }
}