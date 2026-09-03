using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Protocol;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoHubConnectionContextAdditionalTests
{
    private readonly List<RaidoHubConnectionContext> _contexts = new();
    private readonly List<CancellationTokenSource> _connectionClosedSources = new();
    private readonly List<(Pipe Input, Pipe Output)> _transports = new();

    [TestCleanup]
    public async Task CleanupConnections()
    {
        foreach (var source in _connectionClosedSources)
        {
            source.Cancel();
        }

        foreach (var context in _contexts)
        {
            context.Abort();
            await context.CleanupAsync();
        }

        foreach (var source in _connectionClosedSources)
        {
            source.Dispose();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }

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

    private sealed class BlockingProtocolLifetime : IAsyncDisposable
    {
        public TaskCompletionSource DisposeStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseDispose { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask DisposeAsync()
        {
            DisposeStarted.TrySetResult();
            return new ValueTask(ReleaseDispose.Task);
        }
    }

    private sealed class UserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }

    private sealed class TrackingProtocolLifetime : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingProtocolLifetime : IAsyncDisposable
    {
        public InvalidOperationException Exception { get; } = new("Protocol lifetime disposal failed.");

        public ValueTask DisposeAsync() => ValueTask.FromException(Exception);
    }

    private (RaidoHubConnectionContext Context, Pipe Output, FeatureCollection Features, ConnectionContext Connection) CreateContext(TimeSpan? keepAlive = null, TimeSpan? timeout = null)
    {
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        var features = new FeatureCollection();
        var connectionClosed = new CancellationTokenSource();
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns("additional");
        connection.Transport.Returns(transport);
        connection.Features.Returns(features);
        connection.ConnectionClosed.Returns(connectionClosed.Token);
        var context = RaidoTestConnectionFactory.Create(connection, new RaidoConnectionContextOptions
        {
            KeepAliveInterval = keepAlive ?? TimeSpan.FromMinutes(1),
            ClientTimeoutInterval = timeout ?? TimeSpan.FromMinutes(1)
        }, NullLoggerFactory.Instance, protocol: new WritingProtocol());
        _contexts.Add(context);
        _connectionClosedSources.Add(connectionClosed);
        _transports.Add((input, output));
        return (context, output, features, connection);
    }

    [TestMethod]
    public async Task Context_ExposesUnderlyingPropertiesAndWritesMessages()
    {
        var (context, output, features, _) = CreateContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity("test"));
        context.Features.Set<IConnectionUserFeature>(new UserFeature { User = user });
        Assert.AreSame(user, context.User);
        Assert.AreEqual("additional", context.ConnectionId);
        Assert.AreNotSame(features, context.Features);
        Assert.IsNotNull(context.Items);
        await context.OnConnectedAsync();
        await context.WriteAsync(new TestMessage());
        var result = await output.Reader.ReadAsync();
        CollectionAssert.AreEqual(new byte[] { 42 }, result.Buffer.ToArray());
        output.Reader.AdvanceTo(result.Buffer.End);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task Context_AbortIsIdempotentAndCompletesAbortAsync()
    {
        var (context, _, _, _) = CreateContext();
        var abortCallbackCount = 0;
        using var registration = context.ConnectionAborted.Register(() => Interlocked.Increment(ref abortCallbackCount));

        context.Abort();
        await context.AbortAsync();
        context.Abort();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.AbortAsync();
        Assert.AreEqual(1, abortCallbackCount);
    }

    [TestMethod]
    public async Task Context_HandlesWriteFailureAndIgnoresWritesAfterAbort()
    {
        var (context, _, _, _) = CreateContext();
        var protocol = Substitute.For<IRaidoProtocol>();
        protocol.When(x => x.WriteMessage(Arg.Any<RaidoMessage>(), Arg.Any<IBufferWriter<byte>>()))
            .Do(_ => throw new InvalidOperationException("write"));
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        await context.WriteAsync(new TestMessage());
        Assert.IsInstanceOfType<InvalidOperationException>(context.TerminalException);
        context.Abort();
        await context.WriteAsync(new TestMessage());
    }

    [TestMethod]
    public async Task SetProtocolAsync_RejectsNullProtocol()
    {
        var (context, _, _, _) = CreateContext();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => context.SetProtocolAsync(null!, CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task SetProtocolAsync_DisposesOwnedProtocolLifetimeOnReplacementAndCleanup()
    {
        var (context, _, _, _) = CreateContext();
        var firstLifetime = new TrackingProtocolLifetime();
        var secondLifetime = new TrackingProtocolLifetime();

        await context.SetProtocolAsync(new WritingProtocol(), firstLifetime, CancellationToken.None);
        Assert.AreEqual(0, firstLifetime.DisposeCount);
        Assert.AreEqual(0, secondLifetime.DisposeCount);

        await context.SetProtocolAsync(new WritingProtocol(), secondLifetime, CancellationToken.None);
        Assert.AreEqual(1, firstLifetime.DisposeCount);
        Assert.AreEqual(0, secondLifetime.DisposeCount);

        await context.CleanupAsync();
        await context.CleanupAsync();

        Assert.AreEqual(1, firstLifetime.DisposeCount);
        Assert.AreEqual(1, secondLifetime.DisposeCount);
    }

    [TestMethod]
    public async Task CleanupAsync_ReleasesWriteBoundaryWhenProtocolLifetimeDisposalThrows()
    {
        var (context, _, _, _) = CreateContext();
        var lifetime = new ThrowingProtocolLifetime();
        await context.SetProtocolAsync(new WritingProtocol(), lifetime, CancellationToken.None);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => context.CleanupAsync());

        Assert.AreSame(lifetime.Exception, exception);

        var transition = context.SetProtocolAsync(new WritingProtocol(), CancellationToken.None).AsTask();
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            () => transition.WaitAsync(TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public async Task SetProtocolAsync_RejectsOwnedTransitionAfterCleanup()
    {
        var (context, _, _, _) = CreateContext();
        var protocolB = new WritingProtocol();
        var lifetimeB = new TrackingProtocolLifetime();

        await context.CleanupAsync();

        var transition = context.SetProtocolAsync(protocolB, lifetimeB, CancellationToken.None).AsTask();
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            () => transition.WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.AreNotSame(protocolB, context.Protocol);
        Assert.AreEqual(1, lifetimeB.DisposeCount);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task QueuedOwnedProtocolTransitionIsRejectedAfterConnectionBecomesTerminal()
    {
        var (context, _, _, _) = CreateContext();
        var protocolA = new WritingProtocol();
        var protocolB = new WritingProtocol();
        var protocolX = new WritingProtocol();
        var lifetimeA = new BlockingProtocolLifetime();
        var lifetimeB = new TrackingProtocolLifetime();
        Task? firstTransition = null;
        Task? queuedTransition = null;

        try
        {
            await context.SetProtocolAsync(protocolA, lifetimeA, CancellationToken.None);

            firstTransition = context.SetProtocolAsync(protocolX, CancellationToken.None).AsTask();
            await lifetimeA.DisposeStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            queuedTransition = context.SetProtocolAsync(protocolB, lifetimeB, CancellationToken.None).AsTask();
            Assert.IsFalse(queuedTransition.IsCompleted);

            context.Abort();
            lifetimeA.ReleaseDispose.TrySetResult();
            await firstTransition.WaitAsync(TimeSpan.FromSeconds(1));

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                () => queuedTransition.WaitAsync(TimeSpan.FromSeconds(1)));
            Assert.AreSame(protocolX, context.Protocol);
            Assert.AreEqual(1, lifetimeB.DisposeCount);
        }
        finally
        {
            lifetimeA.ReleaseDispose.TrySetResult();
            if (firstTransition is not null)
            {
                await firstTransition;
            }

            if (queuedTransition is not null)
            {
                try
                {
                    await queuedTransition;
                }
                catch (ObjectDisposedException)
                {
                }
            }

        }
    }

    [TestMethod]
    public async Task SetProtocolAsync_KeepsNewProtocolCommittedWhenPreviousLifetimeDisposalThrows()
    {
        var (context, output, _, _) = CreateContext();
        var firstLifetime = new ThrowingProtocolLifetime();
        var secondLifetime = new TrackingProtocolLifetime();
        var protocolB = new WritingProtocol();

        await context.SetProtocolAsync(new WritingProtocol(), firstLifetime, CancellationToken.None);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => context.SetProtocolAsync(protocolB, secondLifetime, CancellationToken.None).AsTask());

        Assert.AreSame(firstLifetime.Exception, exception);
        Assert.AreSame(protocolB, context.Protocol);

        await context.WriteAsync(new TestMessage());
        var result = await output.Reader.ReadAsync();
        CollectionAssert.AreEqual(new byte[] { 42 }, result.Buffer.ToArray());
        output.Reader.AdvanceTo(result.Buffer.End);

        await context.CleanupAsync();
        Assert.AreEqual(1, secondLifetime.DisposeCount);
    }

    [TestMethod]
    public async Task Context_RegistersHeartbeatsAndTimeoutState()
    {
        var (context, _, features, connection) = CreateContext(keepAlive: TimeSpan.Zero, timeout: TimeSpan.Zero);
        var heartbeat = Substitute.For<IConnectionHeartbeatFeature>();
        features.Set(heartbeat);
        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.StartClientTimeout();
        context.StartClientTimeout();
        context.BeginClientTimeout();
        var check = typeof(RaidoHubConnectionContext).GetMethod("CheckClientTimeout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        check.Invoke(context, Array.Empty<object>());
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        context.StopClientTimeout();
    }

}
