using System.Collections.Generic;
using System.Threading;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Hagalaz.AppHost;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.AppHost.Tests;

[TestClass]
public sealed class AppHostDependencyTests
{
    [TestMethod]
    public async Task DatabaseDependentServices_WaitForSuccessfulMigrationCompletion()
    {
        var builder = DistributedApplication.CreateBuilder(["--disable-dashboard"]);
        AppHostConfiguration.Configure(builder, includeHealthChecks: false);

        await using var application = builder.Build();
        var model = application.Services.GetRequiredService<DistributedApplicationModel>();
        var resources = model.GetProjectResources().ToDictionary(resource => resource.Name);
        var migration = resources["hagalaz-database-migrations"];

        foreach (var serviceName in new[]
                 {
                     "hagalaz-services-authorization",
                     "hagalaz-services-characters",
                     "hagalaz-services-contacts",
                     "hagalaz-services-gameworld-1",
                     "hagalaz-services-gameworld-2"
                 })
        {
            var service = resources[serviceName];
            var migrationWait = service.Annotations
                .OfType<WaitAnnotation>()
                .Single(annotation => ReferenceEquals(annotation.Resource, migration));

            Assert.AreEqual(WaitType.WaitForCompletion, migrationWait.WaitType, serviceName);
            Assert.AreEqual(0, migrationWait.ExitCode, serviceName);
        }
    }

    [TestMethod]
    public async Task LocalWorlds_UseProxylessTcpEndpointsOnDistinctLoopbackHosts()
    {
        var builder = DistributedApplication.CreateBuilder(["--disable-dashboard"]);
        AppHostConfiguration.Configure(builder, includeHealthChecks: false);

        await using var application = builder.Build();
        var model = application.Services.GetRequiredService<DistributedApplicationModel>();
        var resources = model.GetProjectResources().ToDictionary(resource => resource.Name);

        foreach (var serviceName in new[] { "hagalaz-services-gameworld-1", "hagalaz-services-gameworld-2" })
        {
            var tcpEndpoint = resources[serviceName].Annotations
                .OfType<EndpointAnnotation>()
                .Single(endpoint => endpoint.Name == "tcp");

            Assert.AreEqual(443, tcpEndpoint.TargetPort, serviceName);
            Assert.AreEqual(443, tcpEndpoint.Port, serviceName);
            Assert.IsFalse(tcpEndpoint.IsProxied, serviceName);
        }
    }

    [TestMethod]
    public async Task Authorization_HasNamedHttpsEndpointAndEndpointDerivedIssuerEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder(["--disable-dashboard"]);
        AppHostConfiguration.Configure(builder, includeHealthChecks: false);

        await using var application = builder.Build();
        var model = application.Services.GetRequiredService<DistributedApplicationModel>();
        var authorization = model.GetProjectResources()
            .Single(resource => resource.Name == "hagalaz-services-authorization");

        var https = authorization.Annotations
            .OfType<EndpointAnnotation>()
            .Single(endpoint => endpoint.Name == "https");
        Assert.AreEqual("https", https.Name);
        Assert.IsTrue(https.IsProxied);

        var environmentAnnotations = authorization.Annotations
            .OfType<EnvironmentCallbackAnnotation>()
            .ToArray();
        Assert.IsTrue(environmentAnnotations.Length > 0);

        var environment = new Dictionary<string, object>();
        var executionContext = application.Services.GetRequiredService<DistributedApplicationExecutionContext>();
        var callbackContext = new EnvironmentCallbackContext(
            executionContext,
            authorization,
            environment,
            CancellationToken.None);
        foreach (var annotation in environmentAnnotations)
        {
            await annotation.Callback(callbackContext);
        }

        Assert.IsTrue(environment.TryGetValue("OpenIddict__Issuer", out var issuer));
        Assert.IsInstanceOfType<EndpointReference>(issuer);
        Assert.AreSame(https, ((EndpointReference)issuer).EndpointAnnotation);
    }
}
