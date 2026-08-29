namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldReconnectCharacterizationTests
{
    private const string FixtureName =
        "Hagalaz.Services.GameWorld.Tests.Fixtures.world-reconnect-characterization.properties";

    [TestMethod]
    public void Revision742Fixture_RecordsObservedOpcode16ReconnectFlagContractWithoutSecrets()
    {
        var fixture = LoadFixture();

        Assert.AreEqual(14, Value(fixture, "handshake.opcode"));
        Assert.AreEqual(16, Value(fixture, "fresh.request.opcode"));
        Assert.AreEqual(0, Value(fixture, "fresh.request.reconnectFlag"));
        Assert.AreEqual(16, Value(fixture, "reconnect.request.opcode"));
        Assert.AreEqual(1, Value(fixture, "reconnect.request.reconnectFlag"));
        Assert.AreEqual(18, Value(fixture, "unsupported.wire.opcode"));
        Assert.AreEqual(742, Value(fixture, "authentication.revision"));
        Assert.AreEqual(1, Value(fixture, "authentication.subrevision"));
        Assert.AreEqual(11, Value(fixture, "authentication.rsa.offset"));
        Assert.AreEqual(96, Value(fixture, "authentication.state.after.reset"));
        Assert.AreEqual(4656, Value(fixture, "fresh.world.entry.bytes"));
        Assert.AreEqual(15, Value(fixture, "reconnect.response"));
        Assert.AreEqual(4608, Value(fixture, "reconnect.world.entry.bytes"));
        Assert.AreEqual("50", fixture["authentication.server.key.transform"]);
        Assert.AreEqual("true", fixture["authentication.protocol.preserved"]);
        Assert.AreEqual("true", fixture["authentication.temporary.keys.cleared"]);
        Assert.AreEqual("true", fixture["authentication.client.isaac.fresh"]);
        Assert.AreEqual("true", fixture["authentication.server.isaac.fresh"]);
        Assert.AreEqual(
            "handshake14,fresh-request16-flag0,response2,fresh-world-entry,map-loaded,transport-disconnect,handshake14,reconnect-request16-flag1,response15,reconnect-world-entry",
            fixture["client.order"]);
    }

    [TestMethod]
    public void Revision742Fixture_DoesNotClaimUnobservedServerHandoffOrResynchronizationOrdering()
    {
        var fixture = LoadFixture();

        Assert.AreEqual("false", fixture["server.transport.adoption.order.characterized"]);
        Assert.AreEqual("false", fixture["server.resynchronization.algorithm.characterized"]);
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
