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

    private static ConnectionContext RawConnection()
    {
        var connection = Substitute.For<ConnectionContext>();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(Substitute.For<PipeReader>());
        transport.Output.Returns(Substitute.For<PipeWriter>());
        connection.Transport.Returns(transport);
        connection.ConnectionId.Returns("extensions");
        connection.ConnectionClosed.Returns(CancellationToken.None);
        return connection;
    }

    [TestMethod]
    public void BuilderExtensions_RegisterProtocolsAndHubs()
    {
        var services = new ServiceCollection();
        var builder = services.AddRaidoProtocol<IRaidoProtocol, SimpleProtocol>(_ => { });
        Assert.AreSame(services, builder);
        Assert.IsNotNull(services.BuildServiceProvider().GetService<IRaidoProtocol>());
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
    public void DefaultContexts_ExposeLifetimeAndCallerState()
    {
        var lifetime = Substitute.For<IRaidoLifetimeManager>();
        var context = new DefaultRaidoContext(lifetime);
        Assert.IsNotNull(context.Clients);
        var connection = new RaidoHubConnectionContext(RawConnection(), new RaidoHubConnectionContextOptions(), NullLoggerFactory.Instance)
        {
            Protocol = new SimpleProtocol()
        };
        var caller = new DefaultRaidoCallerContext(connection);
        Assert.AreEqual(connection.ConnectionId, caller.ConnectionId);
        Assert.AreSame(connection.Items, caller.Items);
        Assert.AreSame(connection.Features, caller.Features);
        Assert.AreEqual(connection.ConnectionAbortedToken, caller.ConnectionAbortedToken);
        Assert.AreSame(connection.Protocol, caller.Protocol);
        caller.Protocol = new SimpleProtocol();
        caller.Abort();
        Assert.IsTrue(connection.ConnectionAbortedToken.CanBeCanceled);
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
