using System.Net;
using System.Net.Sockets;
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
    public async Task LocalWorlds_ExposeTheLegacyClientPortOnDistinctLoopbackHosts()
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

            Assert.AreEqual(443, tcpEndpoint.Port, serviceName);
        }

        using var worldOne = new TcpListener(IPAddress.Parse("127.0.0.1"), 0);
        worldOne.Start();
        var port = ((IPEndPoint)worldOne.LocalEndpoint).Port;
        using var worldTwo = new TcpListener(IPAddress.Parse("127.0.0.2"), port);
        worldTwo.Start();

        var acceptOne = worldOne.AcceptTcpClientAsync();
        var acceptTwo = worldTwo.AcceptTcpClientAsync();
        using var clientOne = new TcpClient();
        using var clientTwo = new TcpClient();
        await clientOne.ConnectAsync(IPAddress.Parse("127.0.0.1"), port);
        await clientTwo.ConnectAsync(IPAddress.Parse("127.0.0.2"), port);

        using var acceptedOne = await acceptOne;
        using var acceptedTwo = await acceptTwo;
        Assert.IsTrue(clientOne.Connected);
        Assert.IsTrue(clientTwo.Connected);
        Assert.IsTrue(acceptedOne.Connected);
        Assert.IsTrue(acceptedTwo.Connected);
    }
}
