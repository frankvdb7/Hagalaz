namespace Hagalaz.AppHost.Telemetry
{
    public class OpenTelemetryCollectorResource(string name) : ContainerResource(name)
    {
        internal const string OtlpGrpcEndpointName = "grpc";
        internal const string OtlpHttpEndpointName = "http";
    }
}