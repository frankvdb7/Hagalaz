using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hagalaz.AppHost
{
    public static class ResourceExtensions
    {
        public static IResourceBuilder<T> WithScalarDocs<T>(this IResourceBuilder<T> builder)
            where T : IResourceWithEndpoints =>
            builder.WithOpenApiDocs("scalar-docs", "API docs", "scalar");

        private static IResourceBuilder<T> WithOpenApiDocs<T>(this IResourceBuilder<T> builder, string name, string displayName, string openApiUiPath)
            where T : IResourceWithEndpoints =>
            builder.WithCommand(name,
                displayName,
                async _ =>
                {
                    await Task.CompletedTask;
                    try
                    {
                        var endpoint = builder.GetEndpoint("https");
                        var url = $"{endpoint.Url}/{openApiUiPath}";
                        Process.Start(new ProcessStartInfo(url)
                        {
                            UseShellExecute = true
                        });
                        return new ExecuteCommandResult
                        {
                            Success = true
                        };
                    }
                    catch (Exception ex)
                    {
                        return new ExecuteCommandResult
                        {
                            Success = false, ErrorMessage = ex.ToString()
                        };
                    }
                },
                new CommandOptions
                {
                    IconName = "Document",
                    IconVariant = IconVariant.Regular,
                    UpdateState = context =>
                        context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy ? ResourceCommandState.Enabled : ResourceCommandState.Disabled
                });
    }
}
