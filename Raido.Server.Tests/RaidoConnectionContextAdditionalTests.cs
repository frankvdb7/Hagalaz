using System.Buffers;
using System.IO.Pipelines;
using System.Security.Claims;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Protocol;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoConnectionContextAdditionalTests
{
    private sealed class WritingProtocol : IRaidoProtocol
    {
        public string Name => "writing";
        public int Version => 1;
        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage message)
        {
            consumed = input.End;
            examined = input.End;
            message = new TestMessage();
            return true;
        }
        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
        {
            output.GetSpan(1)[0] = 42;
            output.Advance(1);
        }
        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => new byte[] { 42 };
        public bool IsVersionSupported(int version) => version == 1;
    }

    private sealed class UserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }

    private static (RaidoConnectionContext Context, Pipe Output, FeatureCollection Features) CreateContext(TimeSpan? keepAlive = null, TimeSpan? timeout = null)
    {
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(Substitute.For<PipeReader>());
        transport.Output.Returns(output.Writer);
        var features = new FeatureCollection();
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns("additional");
        connection.Transport.Returns(transport);
        connection.Features.Returns(features);
        connection.ConnectionClosed.Returns(CancellationToken.None);
        var context = new RaidoConnectionContext(connection, new RaidoConnectionContextOptions
        {
            KeepAliveInterval = keepAlive ?? TimeSpan.FromMinutes(1),
            ClientTimeoutInterval = timeout ?? TimeSpan.FromMinutes(1)
        }, NullLoggerFactory.Instance)
        {
            Protocol = new WritingProtocol()
        };
        return (context, output, features);
    }

    [TestMethod]
    public async Task Context_ExposesUnderlyingPropertiesAndWritesMessages()
    {
        var (context, output, features) = CreateContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity("test"));
        features.Set<IConnectionUserFeature>(new UserFeature { User = user });
        Assert.AreSame(user, context.User);
        Assert.AreEqual("additional", context.ConnectionId);
        Assert.AreSame(features, context.Features);
        Assert.IsNotNull(context.Items);
        await context.OnConnectedAsync();
        await context.WriteAsync(new TestMessage());
        var result = await output.Reader.ReadAsync();
        CollectionAssert.AreEqual(new byte[] { 42 }, result.Buffer.ToArray());
        output.Reader.AdvanceTo(result.Buffer.End);
        context.Cleanup();
    }

    [TestMethod]
    public async Task Context_AbortsAndCompletesAbortAsync()
    {
        var (context, _, _) = CreateContext();
        context.Abort();
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        await context.AbortAsync();
    }

    [TestMethod]
    public async Task Context_HandlesWriteFailureAndIgnoresWritesAfterAbort()
    {
        var (context, _, _) = CreateContext();
        var protocol = Substitute.For<IRaidoProtocol>();
        protocol.When(x => x.WriteMessage(Arg.Any<RaidoMessage>(), Arg.Any<IBufferWriter<byte>>()))
            .Do(_ => throw new InvalidOperationException("write"));
        context.Protocol = protocol;
        await context.WriteAsync(new TestMessage());
        Assert.IsInstanceOfType<InvalidOperationException>(context.CloseException);
        context.Abort();
        await context.WriteAsync(new TestMessage());
    }

    [TestMethod]
    public async Task Context_RegistersHeartbeatsAndTimeoutState()
    {
        var (context, _, features) = CreateContext(keepAlive: TimeSpan.Zero, timeout: TimeSpan.Zero);
        var heartbeat = Substitute.For<IConnectionHeartbeatFeature>();
        features.Set(heartbeat);
        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.StartClientTimeout();
        context.StartClientTimeout();
        context.BeginClientTimeout();
        var check = typeof(RaidoConnectionContext).GetMethod("CheckClientTimeout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        check.Invoke(context, null);
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.StopClientTimeout();
    }
}
