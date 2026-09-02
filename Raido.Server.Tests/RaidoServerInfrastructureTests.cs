using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Buffers;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;
using Raido.Server.Internal.Proxies;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoServerInfrastructureTests
{
    private readonly List<RaidoHubConnectionContext> _connections = new();
    private readonly List<(Pipe Input, Pipe Output)> _transports = new();

    [TestCleanup]
    public void CleanupConnections()
    {
        foreach (var connection in _connections)
        {
            connection.Abort();
            connection.Cleanup();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }

    private sealed class Encoder : IRaidoMessageEncoder<TestMessage>
    {
        public void EncodeMessage(TestMessage message, IRaidoMessageBinaryWriter output) => output.SetOpcode(7);
    }

    private sealed class InvalidEncoder : IRaidoMessageEncoder
    {
        public void EncodeMessage(RaidoMessage message, IRaidoMessageBinaryWriter output) { }
    }

    private sealed class Decoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = new TestMessage();
            return true;
        }
    }

    private sealed class FailingDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = null;
            return false;
        }
    }

    private sealed class Protocol : IRaidoProtocol
    {
        public string Name => "test";
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
        public bool IsVersionSupported(int version) => version == Version;
    }

    private sealed class TestHub : RaidoHub { }

    private RaidoHubConnectionContext CreateConnection(string id)
    {
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns(id);
        var transport = Substitute.For<IDuplexPipe>();
        var input = new Pipe();
        var output = new Pipe();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        connection.Transport.Returns(transport);
        connection.ConnectionClosed.Returns(CancellationToken.None);
        _transports.Add((input, output));
        var context = new RaidoHubConnectionContext(connection, new RaidoHubConnectionContextOptions(), NullLoggerFactory.Instance);
        _connections.Add(context);
        return context;
    }

    [TestMethod]
    public void ConsumableBuffer_TracksWritesConsumptionAndClear()
    {
        using var buffer = new ConsumableArrayBufferWriter(4);
        Assert.AreEqual(0, buffer.UnconsumedWrittenCount);
        buffer.GetSpan(4)[..4].Fill(1);
        buffer.Advance(4);
        Assert.AreEqual(4, buffer.UnconsumedWrittenCount);
        buffer.Consume(2);
        Assert.IsTrue(buffer.WrittenSpan.SequenceEqual(new byte[] { 1, 1 }));
        buffer.GetSpan(3)[..3].Fill(2);
        buffer.Advance(3);
        Assert.AreEqual(5, buffer.UnconsumedWrittenCount);
        buffer.Clear();
        Assert.AreEqual(0, buffer.UnconsumedWrittenCount);
    }

    [TestMethod]
    public void ConsumableBuffer_ValidatesArgumentsAndCanGrow()
    {
        using var buffer = new ConsumableArrayBufferWriter();
        Assert.ThrowsExactly<ArgumentException>(() => new ConsumableArrayBufferWriter(0));
        Assert.ThrowsExactly<ArgumentException>(() => buffer.GetMemory(-1));
        Assert.ThrowsExactly<ArgumentException>(() => buffer.Advance(-1));
        Assert.ThrowsExactly<ArgumentException>(() => buffer.Consume(-1));
        Assert.ThrowsExactly<InvalidOperationException>(() => buffer.Advance(1));
        buffer.GetMemory(1024);
        Assert.IsTrue(buffer.Capacity >= 1024);
        buffer.Advance(1024);
        Assert.ThrowsExactly<InvalidOperationException>(() => buffer.Consume(1025));
    }

    [TestMethod]
    public void CodecStore_RegistersAndRejectsDuplicatesOrInvalidTypes()
    {
        var store = new RaidoCodecStore<Protocol>();
        store.AddDecoder<Decoder>(1);
        store.AddEncoder<Encoder>();
        Assert.IsTrue(store.TryGetDecoder(1, out var decoderType));
        Assert.AreEqual(typeof(Decoder), decoderType);
        Assert.IsTrue(store.TryGetEncoder(typeof(TestMessage), out var encoderType));
        Assert.AreEqual(typeof(Encoder), encoderType);
        Assert.IsFalse(store.TryGetDecoder(2, out _));
        Assert.IsFalse(store.TryGetEncoder(typeof(PingMessage), out _));
        Assert.ThrowsExactly<ArgumentException>(() => store.AddDecoder<Decoder>(1));
        Assert.ThrowsExactly<ArgumentException>(() => store.AddEncoder<Encoder>());
        Assert.ThrowsExactly<TypeAccessException>(() => store.AddEncoder<InvalidEncoder>());
    }

    [TestMethod]
    public void CodecFactoryAndCodec_HandleKnownUnknownAndFailedMessages()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Decoder>();
        services.AddSingleton<FailingDecoder>();
        services.AddSingleton<Encoder>();
        var store = new RaidoCodecStore<Protocol>();
        store.AddDecoder<Decoder>(1);
        store.AddDecoder<FailingDecoder>(3);
        store.AddEncoder<Encoder>();
        using var provider = services.BuildServiceProvider();
        var factory = new DefaultRaidoCodecFactory<Protocol>(provider, store);
        var codec = new DefaultRaidoCodec<Protocol>(factory, NullLogger<DefaultRaidoCodec<Protocol>>.Instance);
        Assert.IsInstanceOfType<Decoder>(factory.GetMessageDecoder(1));
        Assert.IsNull(factory.GetMessageDecoder(2));
        Assert.IsInstanceOfType<Encoder>(factory.GetMessageEncoder(typeof(TestMessage)));
        Assert.IsNull(factory.GetMessageEncoder(typeof(PingMessage)));
        Assert.IsTrue(codec.TryDecodeMessage(1, new ReadOnlySequence<byte>(new byte[] { 1 }), out var decoded));
        Assert.IsInstanceOfType<TestMessage>(decoded);
        Assert.IsFalse(codec.TryDecodeMessage(3, ReadOnlySequence<byte>.Empty, out _));
        Assert.IsFalse(codec.TryDecodeMessage(2, ReadOnlySequence<byte>.Empty, out _));
        Assert.IsTrue(codec.TryEncodeMessage(new TestMessage(), new RaidoMessageBinaryWriter(MemoryBufferWriter.Get())));
        Assert.IsFalse(codec.TryEncodeMessage(PingMessage.Instance, new RaidoMessageBinaryWriter(MemoryBufferWriter.Get())));
    }

    [TestMethod]
    public void ProtocolResolver_IsCaseInsensitiveAndHonorsSupportedList()
    {
        var protocol = new Protocol();
        var resolver = new DefaultRaidoProtocolResolver(new[] { protocol }, NullLogger<DefaultRaidoProtocolResolver>.Instance);
        Assert.AreEqual(1, resolver.AllProtocols.Count);
        Assert.AreSame(protocol, resolver.GetProtocol("TEST", new[] { "test" }));
        Assert.IsNull(resolver.GetProtocol("test", new[] { "other" }));
        Assert.IsNull(resolver.GetProtocol("other", null));
        Assert.ThrowsExactly<ArgumentNullException>(() => resolver.GetProtocol(null!, null));
    }

    [TestMethod]
    public async Task ConnectionStoreAndLifetimeManager_TrackAndSendConnections()
    {
        var first = CreateConnection("one");
        var second = CreateConnection("two");
        var store = new RaidoConnectionStore();
        store.Add(first);
        store.Add(second);
        store.Add(first);
        Assert.AreEqual(2, store.Count);
        Assert.AreSame(first, store["one"]);
        Assert.IsNull(store["missing"]);
        Assert.AreEqual(2, store.ToList().Count);

        var manager = new DefaultRaidoLifetimeManager(store);
        await manager.SendAllAsync(new TestMessage(), CancellationToken.None);
        await manager.SendAllExceptAsync(new TestMessage(), new[] { "one" }, CancellationToken.None);
        await manager.SendConnectionsAsync(new TestMessage(), new[] { "two" }, CancellationToken.None);
        await manager.SendConnectionAsync(new TestMessage(), "one", CancellationToken.None);
        await manager.SendConnectionAsync(new TestMessage(), "missing", CancellationToken.None);
        Assert.ThrowsExactly<ArgumentNullException>(() => manager.SendConnectionAsync(new TestMessage(), null!, CancellationToken.None));
        await manager.OnDisconnectedAsync(first);
        Assert.AreEqual(1, store.Count);
        await manager.OnConnectedAsync(first);
        Assert.AreEqual(2, store.Count);
    }

    [TestMethod]
    public async Task ClientProxies_DelegateToLifetimeManager()
    {
        var manager = Substitute.For<IRaidoLifetimeManager>();
        var message = new TestMessage();
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        await new AllClientProxy(manager).SendAsync(message, token);
        await new AllClientsExceptProxy(manager, new[] { "a" }).SendAsync(message, token);
        await new MultipleClientsProxy(manager, new[] { "a", "b" }).SendAsync(message, token);
        await new SingleClientProxy(manager, "a").SendAsync(message, token);
        await manager.Received().SendAllAsync(message, token);
        await manager.Received().SendAllExceptAsync(message, Arg.Is<IReadOnlyList<string>>(x => x.SequenceEqual(new[] { "a" })), token);
        await manager.Received().SendConnectionsAsync(message, Arg.Is<IReadOnlyList<string>>(x => x.SequenceEqual(new[] { "a", "b" })), token);
        await manager.Received().SendConnectionAsync(message, "a", token);
    }

    [TestMethod]
    public void DefaultClientsAndCallerClients_CreateExpectedProxies()
    {
        var manager = Substitute.For<IRaidoLifetimeManager>();
        var clients = new DefaultRaidoClients(manager);
        var caller = new DefaultRaidoCallerClients(clients, "caller");
        Assert.IsNotNull(clients.All);
        Assert.IsNotNull(clients.AllExcept(new[] { "x" }));
        Assert.IsNotNull(clients.Client("x"));
        Assert.IsNotNull(clients.Clients(new[] { "x" }));
        Assert.IsNotNull(caller.All);
        Assert.IsNotNull(caller.AllExcept(new[] { "x" }));
        Assert.IsNotNull(caller.Client("x"));
        Assert.IsNotNull(caller.Clients(new[] { "x" }));
        Assert.AreNotSame(caller.Caller, caller.Others);
    }

    [TestMethod]
    public void ProtocolBuilder_RegistersCodecsAndServices()
    {
        var services = new ServiceCollection();
        var builder = new DefaultRaidoProtocolBuilder<Protocol>(services);
        Assert.AreSame(services, builder.Services);
        Assert.AreSame(builder, builder.AddDecoder<Decoder>(1));
        Assert.AreSame(builder, builder.AddEncoder<Encoder>());
        using var provider = services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetService<Decoder>());
        Assert.IsNotNull(provider.GetService<Encoder>());
        Assert.IsNotNull(provider.GetService<RaidoCodecStore<Protocol>>());
        Assert.ThrowsExactly<ArgumentNullException>(() => new DefaultRaidoProtocolBuilder<Protocol>(null!));
    }

    [TestMethod]
    public void ServiceRegistration_BuildsCoreRaidoServices()
    {
        var services = new ServiceCollection();
        var builder = services.AddRaidoServerCore();
        Assert.AreSame(services, builder.Services);
        using var provider = services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetRequiredService<RaidoConnectionStore>());
        Assert.IsNotNull(provider.GetRequiredService<IRaidoContext>());
        Assert.IsNotNull(provider.GetRequiredService<IRaidoDispatcher>());
        Assert.IsNotNull(provider.GetRequiredService<IRaidoLifetimeManager>());
        Assert.IsNotNull(provider.GetRequiredService<IRaidoHubConnectionContextBuilder>());
        Assert.ThrowsExactly<ArgumentNullException>(() => ServiceCollectionExtensions.AddRaidoServerCore(null!));
    }
}
