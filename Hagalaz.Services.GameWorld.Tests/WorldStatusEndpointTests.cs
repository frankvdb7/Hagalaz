using Hagalaz.Services.GameWorld.Network.Consumers;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldStatusEndpointTests
{
    [TestMethod]
    public void BroadcastStatusConsumers_AreExcludedFromSharedConfigureEndpoints()
    {
        var consumers = new[]
        {
            typeof(WorldStatusRequestConsumer),
            typeof(WorldOnlineConsumer),
            typeof(WorldOfflineConsumer)
        };

        foreach (var consumer in consumers)
        {
            Assert.IsNotNull(
                consumer.GetCustomAttributes(typeof(ExcludeFromConfigureEndpointsAttribute), inherit: false).SingleOrDefault(),
                consumer.Name);
        }
    }
}
