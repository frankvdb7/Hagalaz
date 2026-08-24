using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Buffers;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoHagalazUsageTests
{
    private sealed class UsageRequest : RaidoMessage
    {
        public byte Value { get; init; }
    }

    private sealed class UsageResponse : RaidoMessage
    {
        public byte Value { get; init; }
    }

    private sealed class UsageProtocol : IRaidoProtocol
    {
        private readonly IRaidoCodec<UsageProtocol> _codec;

        public UsageProtocol(IRaidoCodec<UsageProtocol> codec) => _codec = codec;

        public string Name => "HagalazUsageProtocol";
        public int Version => 742;

        public bool IsVersionSupported(int version) => version == Version;

        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out var opcode) || !reader.TryRead(out var length) || reader.Remaining < length)
            {
                message = null;
                return false;
            }

            var payload = input.Slice(reader.Position, length);
            consumed = payload.End;
            examined = consumed;
            return _codec.TryDecodeMessage(opcode, payload, out message);
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
        {
            using var buffer = MemoryBufferWriter.Get();
            var binaryWriter = new RaidoMessageBinaryWriter(buffer);
            if (!_codec.TryEncodeMessage(message, binaryWriter))
            {
                return;
            }

            var header = output.GetSpan(2);
            header[0] = (byte)binaryWriter.Opcode;
            header[1] = checked((byte)buffer.Length);
            output.Advance(2);
            buffer.CopyTo(output);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
        {
            var output = new ArrayBufferWriter<byte>();
            WriteMessage(message, output);
            return output.WrittenMemory.ToArray();
        }
    }

    private sealed class UsageRequestDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            if (input.Length != 1)
            {
                message = null;
                return false;
            }

            message = new UsageRequest { Value = input.FirstSpan[0] };
            return true;
        }
    }

    private sealed class UsageResponseDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            if (input.Length != 1)
            {
                message = null;
                return false;
            }

            message = new UsageResponse { Value = input.FirstSpan[0] };
            return true;
        }
    }

    private sealed class UsageRequestEncoder : IRaidoMessageEncoder<UsageRequest>
    {
        public void EncodeMessage(UsageRequest message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(1);
            output.WriteByte(message.Value);
        }
    }

    private sealed class UsageResponseEncoder : IRaidoMessageEncoder<UsageResponse>
    {
        public void EncodeMessage(UsageResponse message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(2);
            output.WriteByte(message.Value);
        }
    }

    private interface IUsageService
    {
        byte Transform(byte value);
    }

    private sealed class UsageService : IUsageService
    {
        public byte Transform(byte value) => (byte)(value + 1);
    }

    private sealed class UsageHub : RaidoHub
    {
        private readonly IUsageService _service;

        public UsageHub(IUsageService service) => _service = service;

        [RaidoMessageHandler(typeof(UsageRequest))]
        public UsageResponse Handle(UsageRequest request)
        {
            Context.Items["last-request"] = request.Value;
            return new UsageResponse { Value = _service.Transform(request.Value) };
        }
    }

    [TestMethod]
    public void ServiceRegistration_ResolvesProtocolCodecAndBuilderLikeAServiceHandler()
    {
        using var provider = CreateProvider();
        var protocol = provider.GetRequiredService<UsageProtocol>();
        var encoded = protocol.GetMessageBytes(new UsageRequest { Value = 4 });
        var consumed = default(SequencePosition);
        var examined = default(SequencePosition);

        Assert.IsTrue(protocol.TryParseMessage(new ReadOnlySequence<byte>(encoded.ToArray()), ref consumed, ref examined, out var decoded));
        var request = Assert.IsInstanceOfType<UsageRequest>(decoded);
        Assert.AreEqual((byte)4, request.Value);
        Assert.AreEqual(protocol.Name, provider.GetRequiredService<IRaidoProtocol>().Name);

        var resolver = provider.GetRequiredService<IRaidoProtocolResolver>();
        Assert.IsInstanceOfType<UsageProtocol>(resolver.GetProtocol(protocol.Name.ToLowerInvariant(), new[] { protocol.Name.ToUpperInvariant() }));

        var rawConnection = CreateRawConnection("builder");
        var built = provider.GetRequiredService<IRaidoConnectionContextBuilder>()
            .Create()
            .WithConnection(rawConnection)
            .WithProtocol<UsageProtocol>()
            .Build();

        Assert.AreEqual(protocol.Name, built.Protocol.Name);
        Assert.AreEqual("builder", built.ConnectionId);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task Dispatcher_InvokesInjectedHubServiceAndWritesResponseThroughProtocol()
    {
        using var provider = CreateProvider();
        var protocol = provider.GetRequiredService<UsageProtocol>();
        var (connection, outputReader) = CreateConnection("dispatch", protocol);
        var dispatcher = provider.GetRequiredService<IRaidoDispatcher>();

        await dispatcher.OnConnectedAsync(connection);
        await dispatcher.DispatchMessageAsync(connection, new UsageRequest { Value = 9 });

        var result = await outputReader.ReadAsync();
        CollectionAssert.AreEqual(new byte[] { 2, 1, 10 }, result.Buffer.ToArray());
        Assert.AreEqual((byte)9, connection.Items["last-request"]);

        outputReader.AdvanceTo(result.Buffer.End);
        await dispatcher.OnDisconnectedAsync(connection, null);
        await outputReader.CompleteAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LifetimeManager_BroadcastsAndTargetsConnectionsLikeServiceClientProxies()
    {
        using var provider = CreateProvider();
        var protocol = provider.GetRequiredService<UsageProtocol>();
        var (first, firstOutput) = CreateConnection("first", protocol);
        var (second, secondOutput) = CreateConnection("second", protocol);
        var manager = new DefaultRaidoLifetimeManager(new RaidoConnectionStore());

        await manager.OnConnectedAsync(first);
        await manager.OnConnectedAsync(second);
        await manager.SendAllExceptAsync(new UsageResponse { Value = 5 }, new[] { "first" }, CancellationToken.None);

        CollectionAssert.AreEqual(new byte[] { 2, 1, 5 }, await ReadPacketAsync(secondOutput));
        Assert.IsFalse(secondOutput.TryRead(out _));
        Assert.IsFalse(firstOutput.TryRead(out _));

        await manager.SendConnectionAsync(new UsageResponse { Value = 8 }, "first", CancellationToken.None);
        CollectionAssert.AreEqual(new byte[] { 2, 1, 8 }, await ReadPacketAsync(firstOutput));

        await manager.OnDisconnectedAsync(first);
        await manager.OnDisconnectedAsync(second);
        await firstOutput.CompleteAsync();
        await secondOutput.CompleteAsync();
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        services.AddSingleton<IUsageService, UsageService>();
        services.AddRaidoServer().AddHub<UsageHub>();
        services.AddRaidoProtocol<UsageProtocol>(builder => builder
            .AddDecoder<UsageRequestDecoder>(1)
            .AddDecoder<UsageResponseDecoder>(2)
            .AddEncoder<UsageRequestEncoder>()
            .AddEncoder<UsageResponseEncoder>());
        return services.BuildServiceProvider();
    }

    private static ConnectionContext CreateRawConnection(string connectionId)
    {
        var context = Substitute.For<ConnectionContext>();
        context.ConnectionId.Returns(connectionId);
        context.ConnectionClosed.Returns(CancellationToken.None);
        context.Items.Returns(new Dictionary<object, object?>());
        context.Features.Returns(new Microsoft.AspNetCore.Http.Features.FeatureCollection());
        return context;
    }

    private static (RaidoConnectionContext Connection, PipeReader Output) CreateConnection(string connectionId, IRaidoProtocol protocol)
    {
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);

        var context = CreateRawConnection(connectionId);
        context.Transport.Returns(transport);
        var connection = new RaidoConnectionContext(context, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance)
        {
            Protocol = protocol
        };
        _ = connection.StartPhysicalSession();
        return (connection, output.Reader);
    }

    private static async Task<byte[]> ReadPacketAsync(PipeReader reader)
    {
        var result = await reader.ReadAsync();
        var bytes = result.Buffer.ToArray();
        reader.AdvanceTo(result.Buffer.End);
        return bytes;
    }
}
