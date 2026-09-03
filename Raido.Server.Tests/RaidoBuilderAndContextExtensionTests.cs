using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoBuilderAndContextExtensionTests
{
    private readonly List<RaidoHubConnectionContext> _connections = new();
    private readonly List<(Pipe Input, Pipe Output)> _transports = new();

    [TestCleanup]
    public async Task CleanupConnections()
    {
        foreach (var connection in _connections)
        {
            connection.Abort();
            await connection.CleanupAsync();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }

    private sealed class SimpleProtocol : IRaidoProtocol
    {
        public string Name => "simple";
        public int Version => 1;
        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage message)
        {
            consumed = input.End;
            examined = input.End;
            message = new TestMessage();
            return true;
        }
        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output) { }
        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => ReadOnlyMemory<byte>.Empty;
        public bool IsVersionSupported(int version) => version == 1;
    }

    private sealed class DummyHub : RaidoHub { }

    private ConnectionContext RawConnection()
    {
        var connection = Substitute.For<ConnectionContext>();
        var transport = Substitute.For<IDuplexPipe>();
        var input = new Pipe();
        var output = new Pipe();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        connection.Transport.Returns(transport);
        connection.ConnectionId.Returns("extensions");
        connection.ConnectionClosed.Returns(CancellationToken.None);
        _transports.Add((input, output));
        return connection;
    }

    [TestMethod]
    public void BuilderExtensions_RegisterProtocolsAndHubs()
    {
        var services = new ServiceCollection();
        var builder = services.AddRaidoProtocol<IRaidoProtocol, SimpleProtocol>(_ => { });
        Assert.AreSame(services, builder);
        using (var provider = services.BuildServiceProvider())
        {
            Assert.IsNotNull(provider.GetService<IRaidoProtocol>());
        }
        Assert.ThrowsExactly<ArgumentNullException>(() => RaidoBuilderExtensions.AddRaidoProtocol<IRaidoProtocol, SimpleProtocol>(null!, null));

        var serverBuilder = services.AddRaidoServerCore();
        Assert.AreSame(serverBuilder, serverBuilder.AddHub<DummyHub>());
        Assert.AreSame(serverBuilder, serverBuilder.AddHub<DummyHub>(_ => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => RaidoBuilderExtensions.AddHub<DummyHub>(null!, _ => { }));
    }

    [TestMethod]
    public async Task ConnectionContextExtensions_CreateProtocolAdapters()
    {
        var connection = RawConnection();
        await using var writer = connection.CreateWriter();
        await using var reader = connection.CreateReader();
        using var semaphore = new SemaphoreSlim(1);
        await using var writerWithSemaphore = connection.CreateWriter(semaphore);
        var pipeReader = connection.CreatePipeReader(Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>());
        Assert.IsInstanceOfType<RaidoProtocolWriter>(writer);
        Assert.IsInstanceOfType<RaidoProtocolReader>(reader);
        Assert.IsInstanceOfType<RaidoMessagePipeReader>(pipeReader);

    }

    [TestMethod]
    public async Task DefaultContexts_ExposeLifetimeAndCallerState()
    {
        var lifetime = Substitute.For<IRaidoHubLifetimeManager>();
        var context = new DefaultRaidoContext(lifetime);
        Assert.IsNotNull(context.Clients);
        var connection = RaidoTestConnectionFactory.Create(RawConnection(), new RaidoConnectionContextOptions(), NullLoggerFactory.Instance);
        await connection.SetProtocolAsync(new SimpleProtocol());
        _connections.Add(connection);
        var caller = new DefaultRaidoCallerContext(connection);
        Assert.AreEqual(connection.ConnectionId, caller.ConnectionId);
        Assert.AreSame(connection.Items, caller.Items);
        Assert.AreSame(connection.Features, caller.Features);
        Assert.AreEqual(connection.ConnectionAborted, caller.ConnectionAborted);
        Assert.AreSame(connection.Protocol, caller.Protocol);
        await caller.SetProtocolAsync(new SimpleProtocol());
        caller.Abort();
        Assert.IsTrue(connection.ConnectionAborted.CanBeCanceled);
    }

    [TestMethod]
    public void Hub_DisposeIsIdempotentAndDefaultLifecycleCompletes()
    {
        var hub = new DummyHub();
        hub.OnConnectedAsync().GetAwaiter().GetResult();
        hub.OnDisconnectedAsync(null).GetAwaiter().GetResult();
        hub.Dispose();
        hub.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => hub.Clients);
    }
}
