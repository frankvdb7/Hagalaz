using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoStatefulReconnectTests
{
    [TestMethod]
    public void BuilderOptInUsesTheConfiguredFiniteReconnectTimeout()
    {
        var options = new RaidoOptions { StatefulReconnectTimeout = TimeSpan.FromSeconds(7) };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<RaidoOptions>>(Options.Create(options));
        using var provider = services.BuildServiceProvider();
        var physical = CreatePhysicalConnection("initial");

        var context = new DefaultRaidoConnectionContextBuilder(provider)
            .Create()
            .WithConnection(physical.Connection)
            .WithProtocol(new ReconnectWritingProtocol())
            .WithStatefulReconnect()
            .Build();

        Assert.IsTrue(context.IsReconnectEnabled);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task HandlerUsesAFreshReaderAfterReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.Zero);
        var message = new TestMessage();
        context.Protocol = new TestProtocol { MessageToReturn = message };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("Raido.Server.Tests.Reconnect"));
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            new RaidoMetrics(meterFactory));

        var run = handler.RunAsync(context);
        initial.Closed.Cancel();
        Assert.IsTrue(context.TryReconnect(replacement.Connection));

        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        replacement.Input.Writer.Complete();
        await run.WaitAsync(TimeSpan.FromSeconds(2));

        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task TimeoutWinsWhileCandidateCallbacksAreStillRegistering()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        var heartbeat = new BlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.OnPhysicalConnectionClosed(initial.Connection);

        var reconnect = Task.Run(() => context.TryReconnect(candidate.Connection));
        await heartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsFalse(await context.WaitForReconnectAsync(TimeSpan.Zero));
        heartbeat.Release.TrySetResult();
        Assert.IsFalse(await reconnect.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(candidate.Closed.IsCancellationRequested);
        Assert.IsTrue(context.ConnectionAbortedToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1)));
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ConcurrentCandidatesHaveExactlyOnePublishedWinner()
    {
        var initial = CreatePhysicalConnection("initial");
        var first = CreatePhysicalConnection("first");
        var second = CreatePhysicalConnection("second");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.OnPhysicalConnectionClosed(initial.Connection);

        var results = await Task.WhenAll(
            Task.Run(() => context.TryReconnect(first.Connection)),
            Task.Run(() => context.TryReconnect(second.Connection)));

        Assert.AreEqual(1, results.Count(result => result));
        Assert.IsTrue(context.TryGetCurrentConnection(out var current));
        Assert.IsTrue(ReferenceEquals(current, first.Connection) || ReferenceEquals(current, second.Connection));
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LateCandidateFromTheFirstWindowCannotPublishIntoTheSecondWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var lateCandidate = CreatePhysicalConnection("late");
        var winner = CreatePhysicalConnection("winner");
        var next = CreatePhysicalConnection("next");
        var blockingHeartbeat = new BlockingHeartbeatFeature();
        lateCandidate.Connection.Features.Set<IConnectionHeartbeatFeature>(blockingHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.OnPhysicalConnectionClosed(initial.Connection);

        var lateReconnect = Task.Run(() => context.TryReconnect(lateCandidate.Connection));
        await blockingHeartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TryReconnect(winner.Connection));
        context.OnPhysicalConnectionClosed(winner.Connection);

        blockingHeartbeat.Release.TrySetResult();
        Assert.IsFalse(await lateReconnect.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TryReconnect(next.Connection));
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task StaleAsynchronousWriteFailureCannotAbortTheReplacement()
    {
        var pendingWriter = new DeferredFailingPipeWriter();
        var initial = CreatePhysicalConnection("initial", outputWriter: pendingWriter);
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var pendingWrite = context.WriteAsync(new TestMessage());
        context.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TryReconnect(replacement.Connection));

        var exception = new InvalidOperationException("stale flush");
        pendingWriter.Fail(exception);
        await pendingWrite;

        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsTrue(context.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        context.Cleanup();
    }

    [TestMethod]
    public async Task DisabledConnectionTerminatesImmediatelyOnPhysicalLoss()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: false);

        initial.Closed.Cancel();

        Assert.IsTrue(context.ConnectionAbortedToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TryReconnect(CreatePhysicalConnection("replacement").Connection));
        context.Cleanup();
        await context.AbortAsync();
    }

    [TestMethod]
    public void DetachedConnectionHasNoEndpointsAndDoesNotExposePipes()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsNull(context.LocalEndPoint);
        Assert.IsNull(context.RemoteEndPoint);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = context.Input);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = context.Output);
        context.Cleanup();
    }

    [TestMethod]
    public async Task ExplicitAbortWhileDetachedCompletesTheReconnectWindowAndStaysTerminal()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);
        var reconnectWindow = context.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        context.Abort();

        Assert.IsFalse(await reconnectWindow.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsTrue(context.ConnectionAbortedToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TryReconnect(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public void AlreadyClosedCandidateIsRejectedWithoutPublishingATransport()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        var closeRequested = new CloseRequestedFeature();
        candidate.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        closeRequested.RequestClose();
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsFalse(context.TryReconnect(candidate.Connection));
        Assert.IsFalse(context.TryGetCurrentConnection(out _));
        Assert.IsTrue(closeRequested.ConnectionClosedRequested.IsCancellationRequested);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void RejectedCandidateRemainsOwnedByTheCaller()
    {
        var initial = CreatePhysicalConnection("initial");
        var winner = CreatePhysicalConnection("winner");
        var rejected = CreatePhysicalConnection("rejected");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TryReconnect(winner.Connection));

        Assert.IsFalse(context.TryReconnect(rejected.Connection));
        Assert.IsFalse(rejected.Closed.IsCancellationRequested);
        Assert.IsTrue(context.TryGetCurrentConnection(out var current));
        Assert.AreSame(winner.Connection, current);
        context.Cleanup();
    }

    [TestMethod]
    public async Task CurrentWriteFailureDetachesAndAllowsAReplacement()
    {
        var pendingWriter = new DeferredFailingPipeWriter();
        var initial = CreatePhysicalConnection("initial", outputWriter: pendingWriter);
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var pendingWrite = context.WriteAsync(new TestMessage());
        var exception = new InvalidOperationException("current flush");
        pendingWriter.Fail(exception);
        await pendingWrite;

        Assert.IsFalse(context.TryGetCurrentConnection(out _));
        Assert.AreSame(exception, context.CloseException);

        var reconnectWindow = context.WaitForReconnectAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(context.TryReconnect(replacement.Connection));
        Assert.IsTrue(await reconnectWindow);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void StaleTransportFailureCannotDetachTheReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TryReconnect(replacement.Connection));

        var exception = new InvalidOperationException("stale read");
        Assert.IsTrue(context.HandleTransportFailure(initial.Connection, exception));

        Assert.IsTrue(context.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsNull(context.CloseException);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public async Task CurrentReadFailureDetachesAndHandlerContinuesWithTheReplacement()
    {
        var readFailure = new InvalidOperationException("current read");
        var failingReader = new ThrowingPipeReader(readFailure);
        var initial = CreatePhysicalConnection("initial", inputReader: failingReader);
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var message = new TestMessage();
        context.Protocol = new TestProtocol { MessageToReturn = message };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, message).Returns(_ =>
        {
            dispatched.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("Raido.Server.Tests.ReadFailure"));
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            new RaidoMetrics(meterFactory));

        var run = handler.DispatchMessagesAsync(context);

        Assert.IsFalse(context.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TryReconnect(replacement.Connection));
        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        replacement.Input.Writer.Complete();
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.Abort();

        await run.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreSame(readFailure, context.CloseException);
        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task CandidateClosingDuringRegistrationIsRejectedAndLeavesTheWindowOpen()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        var winner = CreatePhysicalConnection("winner");
        var heartbeat = new BlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);
        var candidateReconnect = Task.Run(() => context.TryReconnect(candidate.Connection));
        await heartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        candidate.Closed.Cancel();
        heartbeat.Release.TrySetResult();

        Assert.IsFalse(await candidateReconnect.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TryGetCurrentConnection(out _));

        var reconnectWindow = context.WaitForReconnectAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(context.TryReconnect(winner.Connection));
        Assert.IsTrue(await reconnectWindow);
        context.Cleanup();
    }

    [TestMethod]
    public void ClientTimeoutHeartbeatMovesToTheReplacementAndIgnoresTheStaleHeartbeat()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.StartClientTimeout();
        context.BeginClientTimeout();
        context.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TryReconnect(replacement.Connection));

        Assert.AreEqual(2, initialHeartbeat.Callbacks.Count);
        Assert.AreEqual(2, replacementHeartbeat.Callbacks.Count);

        initialHeartbeat.Run();

        Assert.IsTrue(context.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public async Task ConnectAsyncKeepsLifetimeCallbacksStableAcrossReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var message = new TestMessage();
        context.Protocol = new TestProtocol { MessageToReturn = message };
        var lifetimeManager = Substitute.For<IRaidoLifetimeManager>();
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, message).Returns(_ =>
        {
            dispatched.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("Raido.Server.Tests.ReconnectLifetime"));
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            new RaidoMetrics(meterFactory));

        var run = handler.ConnectAsync(context);
        initial.Closed.Cancel();
        Assert.IsTrue(context.TryReconnect(replacement.Connection));
        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        replacement.Input.Writer.Complete();
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.Abort();

        await run.WaitAsync(TimeSpan.FromSeconds(2));

        await lifetimeManager.Received(1).OnConnectedAsync(context);
        await lifetimeManager.Received(1).OnDisconnectedAsync(context);
        await dispatcher.Received(1).OnConnectedAsync(context);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
        await dispatcher.Received(1).DispatchMessageAsync(context, message);
    }

    private sealed class ReconnectWritingProtocol : IRaidoProtocol
    {
        public string Name => "reconnect";
        public int Version => 1;

        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out RaidoMessage message)
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

    [TestMethod]
    public async Task DetachedReadIsWokenWithoutCancellingTheStableAbortToken()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var pendingRead = initial.Input.Reader.ReadAsync().AsTask();
        context.OnPhysicalConnectionClosed(initial.Connection);

        var result = await pendingRead.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        initial.Input.Reader.AdvanceTo(result.Buffer.End);
        context.Cleanup();
    }

    [TestMethod]
    public async Task DetachedWriteDoesNotTouchAProductionPipe()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);
        await context.WriteAsync(new TestMessage());

        Assert.IsFalse(initial.Output.Reader.TryRead(out _));
        context.Cleanup();
    }

    [TestMethod]
    public async Task ReplacementPreservesStableStateAndUsesReplacementTransport()
    {
        var initial = CreatePhysicalConnection("initial", localPort: 1001, remotePort: 2001);
        var replacement = CreatePhysicalConnection("replacement", localPort: 1002, remotePort: 2002);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableFeature = new object();
        context.Features.Set(stableFeature);
        context.Items["state"] = stableFeature;
        var protocol = context.Protocol;

        context.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsTrue(context.TryReconnect(replacement.Connection));
        Assert.AreEqual("initial", context.ConnectionId);
        Assert.AreSame(stableFeature, context.Features.Get<object>());
        Assert.AreSame(stableFeature, context.Items["state"]);
        Assert.AreSame(protocol, context.Protocol);
        Assert.AreEqual(1002, context.LocalEndPoint!.Port);
        Assert.AreEqual(2002, context.RemoteEndPoint!.Port);

        await context.WriteAsync(new TestMessage());

        var result = await replacement.Output.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(42, result.Buffer.FirstSpan[0]);
        replacement.Output.Reader.AdvanceTo(result.Buffer.End);
        context.Cleanup();
    }

    [TestMethod]
    public async Task ReconnectWindowIsReusedForFailedCandidateAndRecreatedAfterLaterDisconnect()
    {
        var initial = CreatePhysicalConnection("initial");
        var failedCandidate = CreatePhysicalConnection("failed");
        var successfulCandidate = CreatePhysicalConnection("successful");
        var laterCandidate = CreatePhysicalConnection("later");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);
        var firstWindow = context.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        failedCandidate.Closed.Cancel();
        Assert.IsFalse(context.TryReconnect(failedCandidate.Connection));
        Assert.IsTrue(context.TryReconnect(successfulCandidate.Connection));
        Assert.IsTrue(await firstWindow);

        context.OnPhysicalConnectionClosed(successfulCandidate.Connection);
        var secondWindow = context.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(ReferenceEquals(firstWindow, secondWindow));
        Assert.IsTrue(context.TryReconnect(laterCandidate.Connection));
        Assert.IsTrue(await secondWindow);
        context.Cleanup();
    }

    [TestMethod]
    public async Task TimedOutReconnectWindowIsTerminalAndCannotBeReopened()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.Zero);

        context.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsFalse(await context.WaitForReconnectAsync(TimeSpan.Zero));
        Assert.IsTrue(context.ConnectionAbortedToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TryReconnect(replacement.Connection));
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void StaleCloseCallbackCannotDetachReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TryReconnect(replacement.Connection));

        initial.Closed.Cancel();

        Assert.AreEqual(1000, context.LocalEndPoint!.Port);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void PhysicalCallbacksAreRegisteredOnTheReplacementAndStaleHeartbeatIsIgnored()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        var closeRequested = new CloseRequestedFeature();
        replacement.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TryReconnect(replacement.Connection));

        Assert.AreEqual(1, initialHeartbeat.Callbacks.Count);
        Assert.AreEqual(1, replacementHeartbeat.Callbacks.Count);
        initialHeartbeat.Run();
        Assert.IsTrue(context.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);

        closeRequested.Source.Cancel();

        Assert.IsFalse(context.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    private static RaidoConnectionContext CreateContext(
        ConnectionContext connection,
        bool reconnectEnabled,
        TimeSpan? timeout = null)
    {
        return new RaidoConnectionContext(connection, new RaidoConnectionContextOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1),
            ClientTimeoutInterval = TimeSpan.FromMinutes(1),
            StatefulReconnectEnabled = reconnectEnabled,
            StatefulReconnectTimeout = timeout ?? TimeSpan.FromSeconds(5)
        }, NullLoggerFactory.Instance)
        {
            Protocol = new ReconnectWritingProtocol()
        };
    }

    private static PhysicalConnection CreatePhysicalConnection(
        string id,
        int localPort = 1000,
        int remotePort = 2000,
        PipeWriter? outputWriter = null,
        PipeReader? inputReader = null)
    {
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(inputReader ?? input.Reader);
        transport.Output.Returns(outputWriter ?? output.Writer);

        var features = new FeatureCollection();
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns(id);
        connection.Transport.Returns(transport);
        connection.Features.Returns(features);
        connection.Items.Returns(new Dictionary<object, object?>());
        connection.LocalEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, localPort));
        connection.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, remotePort));

        var closed = new CancellationTokenSource();
        connection.ConnectionClosed.Returns(closed.Token);
        return new PhysicalConnection(connection, input, output, closed);
    }

    private sealed class DeferredFailingPipeWriter : PipeWriter
    {
        private readonly ArrayBufferWriter<byte> _buffer = new();
        private readonly TaskCompletionSource<FlushResult> _flush =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Fail(Exception exception) => _flush.TrySetException(exception);

        public override void Advance(int bytes) => _buffer.Advance(bytes);

        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

        public override void CancelPendingFlush() { }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => new(_flush.Task);

        public override void Complete(Exception? exception = null) { }
    }

    private sealed class ThrowingPipeReader : PipeReader
    {
        private readonly Exception _exception;

        public ThrowingPipeReader(Exception exception) => _exception = exception;

        public override void AdvanceTo(SequencePosition consumed) { }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { }

        public override void CancelPendingRead() { }

        public override void Complete(Exception? exception = null) { }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => throw _exception;

        public override bool TryRead(out ReadResult result)
        {
            result = default;
            throw _exception;
        }
    }

    private sealed record PhysicalConnection(
        ConnectionContext Connection,
        Pipe Input,
        Pipe Output,
        CancellationTokenSource Closed);

    private sealed class RecordingHeartbeatFeature : IConnectionHeartbeatFeature
    {
        public List<(Action<object> Callback, object State)> Callbacks { get; } = new();

        public void OnHeartbeat(Action<object> action, object state) => Callbacks.Add((action, state));

        public void Run()
        {
            foreach (var (callback, state) in Callbacks)
            {
                callback(state);
            }
        }
    }

    private sealed class BlockingHeartbeatFeature : IConnectionHeartbeatFeature
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnHeartbeat(Action<object> action, object state)
        {
            Entered.TrySetResult();
            Release.Task.GetAwaiter().GetResult();
        }
    }

    private sealed class CloseRequestedFeature : IConnectionLifetimeNotificationFeature
    {
        public CancellationTokenSource Source { get; } = new();

        public CancellationToken ConnectionClosedRequested { get; set; }

        public CloseRequestedFeature() => ConnectionClosedRequested = Source.Token;

        public void RequestClose()
        {
            Source.Cancel();
        }
    }
}
