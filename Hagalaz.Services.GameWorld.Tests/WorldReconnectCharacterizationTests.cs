namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldReconnectCharacterizationTests
{
    private const string FixtureName =
        "Hagalaz.Services.GameWorld.Tests.Fixtures.world-reconnect-characterization.properties";

    [TestMethod]
    public void Revision742Fixture_RecordsObservedRequestsStimuliAndClientObservationsWithoutSecrets()
    {
        var fixture = LoadFixture();

        Assert.AreEqual("true", fixture["evidence.controlledPeer"]);
        Assert.AreEqual("false", fixture["evidence.productionServerCompatibility"]);

        // Observed client requests and trace facts.
        Assert.AreEqual(14, Value(fixture, "controlled.peer.handshake.opcode"));
        Assert.AreEqual(3, Value(fixture, "controlled.peer.wire.header.bytes"));
        Assert.AreEqual(16, Value(fixture, "authentication.header.fresh.opcode"));
        Assert.AreEqual(0, Value(fixture, "authentication.header.fresh.reconnectFlag"));
        Assert.AreEqual(16, Value(fixture, "authentication.header.reconnect.opcode"));
        Assert.AreEqual(1, Value(fixture, "authentication.header.reconnect.reconnectFlag"));

        // Controlled-peer stimuli.
        Assert.AreEqual(2, Value(fixture, "controlled.peer.fresh.response"));
        Assert.AreEqual(15, Value(fixture, "controlled.peer.reconnect.response"));

        // Client-side observations.
        Assert.AreEqual(4608, Value(fixture, "client.reconnect.worldEntry.readBytes"));
        Assert.AreEqual(4656, Value(fixture, "client.fresh.worldEntry.readBytes"));
        Assert.AreEqual(742, Value(fixture, "client.authentication.revision"));
        Assert.AreEqual(1, Value(fixture, "client.authentication.subrevision"));
        Assert.AreEqual(11, Value(fixture, "client.authentication.rsa.offset"));
        Assert.AreEqual("true", fixture["client.authentication.rsa.xtea.boundaries.observed"]);
        Assert.AreEqual("true", fixture["client.authentication.reset.observed"]);
        Assert.AreEqual(96, Value(fixture, "client.authentication.state.after.reset"));
        Assert.AreEqual("50", fixture["client.authentication.server.key.transform"]);
        Assert.AreEqual("true", fixture["client.authentication.protocol.preserved"]);
        Assert.AreEqual("true", fixture["client.authentication.temporary.keys.cleared"]);
        Assert.AreEqual("true", fixture["client.authentication.client.isaac.fresh"]);
        Assert.AreEqual("true", fixture["client.authentication.server.isaac.fresh"]);
        Assert.AreEqual(
            "handshake14,fresh-request16-flag0,response2,fresh-world-entry,map-loaded,transport-disconnect,handshake14,reconnect-request16-flag1,response15,reconnect-world-entry",
            fixture["client.authentication.event.order"]);
        Assert.IsNull(fixture["client.authentication.rsa.ciphertext"]);
        Assert.IsNull(fixture["client.authentication.xtea.key"]);
        Assert.IsNull(fixture["client.authentication.client.isaac.key"]);
        Assert.IsNull(fixture["client.authentication.server.isaac.key"]);
    }

    [TestMethod]
    public void Revision742Fixture_SeparatesTraceFactsClientCodeFactsAndUnknownProductionBehavior()
    {
        var fixture = LoadFixture();

        // Observed client request and trace fact.
        Assert.AreEqual("false", fixture["authentication.header.opcode18.observed"]);

        // Facts discovered from client code.
        Assert.AreEqual("false", fixture["authentication.header.opcode18.presentInRegistry"]);
        Assert.AreEqual("true", fixture["game.channel.opcode18.exists"]);
        Assert.AreEqual("isaac", fixture["game.channel.opcode18.framing"]);
        Assert.AreEqual("false", fixture["game.channel.opcode18.observedInReconnectTrace"]);

        // Unknown production-server behavior.
        Assert.AreEqual("false", fixture["production.server.response15.acceptance.known"]);
        Assert.AreEqual("false", fixture["production.server.reconnect.worldEntryPayload.known"]);
        Assert.AreEqual("false", fixture["production.server.handoff.order.known"]);
        Assert.AreEqual("false", fixture["production.server.resynchronization.order.known"]);
        Assert.AreEqual("false", fixture["production.server.cipher.transition.known"]);
        Assert.AreEqual("false", fixture["production.server.authentication.behavior.known"]);
        Assert.AreEqual("false", fixture["production.server.session.behavior.known"]);
        Assert.AreEqual("false", fixture["production.server.resumed.reads.writes.known"]);
        Assert.IsNull(fixture["reconnect.response"]);
        Assert.IsNull(fixture["reconnect.world.entry.bytes"]);
    }

    private static System.Collections.Specialized.NameValueCollection LoadFixture()
    {
        using var stream = typeof(WorldReconnectCharacterizationTests).Assembly
            .GetManifestResourceStream(FixtureName);
        Assert.IsNotNull(stream);

        var fixture = new System.Collections.Specialized.NameValueCollection();
        using var reader = new StreamReader(stream!);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            Assert.IsTrue(separator > 0, $"Invalid characterization fixture line: {line}");
            fixture[line[..separator]] = line[(separator + 1)..];
        }

        return fixture;
    }

    private static int Value(System.Collections.Specialized.NameValueCollection fixture, string key) =>
        int.Parse(fixture[key] ?? throw new AssertFailedException($"Missing fixture key '{key}'."));
}
