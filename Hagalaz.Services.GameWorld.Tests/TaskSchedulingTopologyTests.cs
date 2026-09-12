using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class TaskSchedulingTopologyTests
{
    [TestMethod]
    public void Startup_UsesOneSchedulerForCreatureQueueAndGameWorkerTick()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:cache"] = "localhost:6379"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        new Startup(configuration).ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        var workerScheduler = provider.GetRequiredService<IRsTaskService>();
        var creatureScheduler = provider.GetRequiredService<ICreatureTaskService>();

        Assert.IsTrue(ReferenceEquals(workerScheduler, creatureScheduler));

        var executed = false;
        creatureScheduler.Schedule(new RsTask(() => executed = true, executeDelay: 1));
        workerScheduler.Tick();

        Assert.IsTrue(executed);
    }
}
