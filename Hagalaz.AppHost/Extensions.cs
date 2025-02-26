using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                iconName: "Document",
                iconVariant: IconVariant.Regular,
                executeCommand: async _ =>
                {
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
                updateState: context =>
                    context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy ? ResourceCommandState.Enabled : ResourceCommandState.Disabled);

        public static IResourceBuilder<TResource> RunWithHttpsDevCertificate<TResource>(
            this IResourceBuilder<TResource> builder, string certFileEnv, string certKeyFileEnv, Action<string, string>? onSuccessfulExport = null)
            where TResource : IResourceWithEnvironment
        {
            if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
            {
                builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(async (e, ct) =>
                {
                    var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(builder.Resource);

                    // Export the ASP.NET Core HTTPS development certificate & private key to files and configure the resource to use them via
                    // the specified environment variables.
                    var (exported, certPath, certKeyPath) = await TryExportDevCertificateAsync(builder.ApplicationBuilder, logger);

                    if (!exported)
                    {
                        // The export failed for some reason, don't configure the resource to use the certificate.
                        return;
                    }

                    if (builder.Resource is ContainerResource containerResource)
                    {
                        // Bind-mount the certificate files into the container.
                        const string devCertBindMountDestDir = "/dev-certs";

                        var certFileName = Path.GetFileName(certPath);
                        var certKeyFileName = Path.GetFileName(certKeyPath);

                        var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

                        var certFileDest = $"{devCertBindMountDestDir}/{certFileName}";
                        var certKeyFileDest = $"{devCertBindMountDestDir}/{certKeyFileName}";

                        builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                            .WithBindMount(bindSource, devCertBindMountDestDir, isReadOnly: true)
                            .WithEnvironment(certFileEnv, certFileDest)
                            .WithEnvironment(certKeyFileEnv, certKeyFileDest);
                    }
                    else
                    {
                        builder
                            .WithEnvironment(certFileEnv, certPath)
                            .WithEnvironment(certKeyFileEnv, certKeyPath);
                    }

                    onSuccessfulExport?.Invoke(certPath, certKeyPath);
                });
            }

            return builder;
        }

        private static async Task<(bool, string CertFilePath, string CertKeyFilPath)> TryExportDevCertificateAsync(
            IDistributedApplicationBuilder builder, ILogger logger)
        {
            // Exports the ASP.NET Core HTTPS development certificate & private key to PEM files using 'dotnet dev-certs https' to a temporary
            // directory and returns the path.
            // TODO: Check if we're running on a platform that already has the cert and key exported to a file (e.g. macOS) and just use those instead.
            var appNameHash = builder.Configuration["AppHost:Sha256"]![..10];
            var tempDir = Path.Combine(Path.GetTempPath(), $"aspire.{appNameHash}");
            var certExportPath = Path.Combine(tempDir, "dev-cert.pem");
            var certKeyExportPath = Path.Combine(tempDir, "dev-cert.key");

            if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
            {
                // Certificate already exported, return the path.
                logger.LogDebug("Using previously exported dev cert files '{CertPath}' and '{CertKeyPath}'", certExportPath, certKeyExportPath);
                return (true, certExportPath, certKeyExportPath);
            }

            if (File.Exists(certExportPath))
            {
                logger.LogTrace("Deleting previously exported dev cert file '{CertPath}'", certExportPath);
                File.Delete(certExportPath);
            }

            if (File.Exists(certKeyExportPath))
            {
                logger.LogTrace("Deleting previously exported dev cert key file '{CertKeyPath}'", certKeyExportPath);
                File.Delete(certKeyExportPath);
            }

            if (!Directory.Exists(tempDir))
            {
                logger.LogTrace("Creating directory to export dev cert to '{ExportDir}'", tempDir);
                Directory.CreateDirectory(tempDir);
            }

            string[] args = ["dev-certs", "https", "--export-path", $"\"{certExportPath}\"", "--format", "Pem", "--no-password"];
            var argsString = string.Join(' ', args);

            logger.LogTrace("Running command to export dev cert: {ExportCmd}", $"dotnet {argsString}");
            var exportStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = argsString,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var exportProcess = new Process
            {
                StartInfo = exportStartInfo
            };

            Task? stdOutTask = null;
            Task? stdErrTask = null;

            try
            {
                try
                {
                    if (exportProcess.Start())
                    {
                        stdOutTask = ConsumeOutput(exportProcess.StandardOutput, msg => logger.LogInformation("> {StandardOutput}", msg));
                        stdErrTask = ConsumeOutput(exportProcess.StandardError, msg => logger.LogError("! {ErrorOutput}", msg));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start HTTPS dev certificate export process");
                    return default;
                }

                var timeout = TimeSpan.FromSeconds(5);
                var exited = exportProcess.WaitForExit(timeout);

                if (exited && File.Exists(certExportPath) && File.Exists(certKeyExportPath))
                {
                    logger.LogDebug("Dev cert exported to '{CertPath}' and '{CertKeyPath}'", certExportPath, certKeyExportPath);
                    return (true, certExportPath, certKeyExportPath);
                }

                if (exportProcess.HasExited && exportProcess.ExitCode != 0)
                {
                    logger.LogError("HTTPS dev certificate export failed with exit code {ExitCode}", exportProcess.ExitCode);
                }
                else if (!exportProcess.HasExited)
                {
                    exportProcess.Kill(true);
                    logger.LogError("HTTPS dev certificate export timed out after {TimeoutSeconds} seconds", timeout.TotalSeconds);
                }
                else
                {
                    logger.LogError("HTTPS dev certificate export failed for an unknown reason");
                }

                return default;
            }
            finally
            {
                await Task.WhenAll(stdOutTask ?? Task.CompletedTask, stdErrTask ?? Task.CompletedTask);
            }

            static async Task ConsumeOutput(TextReader reader, Action<string> callback)
            {
                char[] buffer = new char[256];
                int charsRead;

                while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    callback(new string(buffer, 0, charsRead));
                }
            }
        }
    }
}