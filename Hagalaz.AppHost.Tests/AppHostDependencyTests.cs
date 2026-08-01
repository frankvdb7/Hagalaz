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
        AppHostConfiguration.Configure(builder);

        await using var application = builder.Build();
        var model = application.Services.GetRequiredService<DistributedApplicationModel>();
        var resources = model.GetProjectResources().ToDictionary(resource => resource.Name);
        var migration = resources["hagalaz-database-migrations"];

        foreach (var serviceName in new[]
                 {
                     "hagalaz-services-authorization",
                     "hagalaz-services-characters",
                     "hagalaz-services-contacts",
                     "hagalaz-services-gameworld"
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
}
