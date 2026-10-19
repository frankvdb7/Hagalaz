using System.Linq;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoMetricsTests
{
    [TestMethod]
    public void RaidoMetrics_DeclaresOperationalInstrumentsWithTheirUnits()
    {
        using var metrics = new RaidoMetricsTestContext();
        var instruments = metrics.Listener.Instruments.ToDictionary(instrument => instrument.Name);

        Assert.AreEqual("{message}", instruments["raido.server.inbound_messages"].Unit);
        Assert.AreEqual("s", instruments["raido.server.inbound_message.duration"].Unit);
        Assert.AreEqual("{message}", instruments["raido.server.outbound_messages"].Unit);
        Assert.AreEqual("s", instruments["raido.server.outbound_message.duration"].Unit);
        Assert.AreEqual("{event}", instruments["raido.server.reconnect.events"].Unit);
        Assert.AreEqual("s", instruments["raido.server.reconnect.duration"].Unit);
        Assert.AreEqual("{connection}", instruments["raido.server.client_timeouts"].Unit);
    }
}
