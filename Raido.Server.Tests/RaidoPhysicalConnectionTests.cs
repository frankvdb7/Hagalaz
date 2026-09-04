using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoPhysicalConnectionTests
{
    private readonly List<RaidoHubConnectionContext> _connections = new();
    private readonly List<PhysicalConnection> _physicalConnections = new();
    private readonly List<BlockingHeartbeatFeature> _blockingHeartbeatFeatures = new();
    private readonly List<BlockingPipeWriter> _blockingPipeWriters = new();

    [TestCleanup]
    public async Task DisposePhysicalConnections()
    {
        foreach (var heartbeatFeature in _blockingHeartbeatFeatures)
        {
            heartbeatFeature.Release.TrySetResult();
        }

        foreach (var pipeWriter in _blockingPipeWriters)
        {
            pipeWriter.Release();
        }

        foreach (var connection in _connections)
        {
            connection.Abort();
            await connection.CleanupAsync();
        }

        foreach (var physicalConnection in _physicalConnections)
        {
            physicalConnection.Dispose();
        }

        _connections.Clear();
        _physicalConnections.Clear();
        _blockingHeartbeatFeatures.Clear();
        _blockingPipeWriters.Clear();
    }

    [TestMethod]
    public async Task FactoryOptInUsesTheConfiguredFiniteStatefulReconnectTimeout()
    {
        var options = new RaidoOptions { StatefulReconnectTimeout = TimeSpan.FromSeconds(7) };
        var physical = CreatePhysicalConnection("initial");

        var factory = new DefaultRaidoHubConnectionContextFactory(
            NullLoggerFactory.Instance,
            Options.Create(options));
        var context = factory.Create(physical.Connection, new PhysicalConnectionWritingProtocol(), statefulReconnect: true);
        _connections.Add(context);

        Assert.IsTrue(context.IsReconnectEnabled);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task FactoryOptInUsesTheDefaultFiniteStatefulReconnectTimeout()
    {
        var options = new RaidoOptions();
        var physical = CreatePhysicalConnection("initial");

        var factory = new DefaultRaidoHubConnectionContextFactory(
            NullLoggerFactory.Instance,
            Options.Create(options));
        var context = factory.Create(physical.Connection, new PhysicalConnectionWritingProtocol(), statefulReconnect: true);
        _connections.Add(context);

        Assert.IsTrue(context.IsReconnectEnabled);
        await context.CleanupAsync();
    }

    [TestMethod]
    public void InfiniteStatefulReconnectTimeoutIsRejectedWhenReconnectIsEnabled() =>
        AssertStatefulReconnectTimeoutRejected(Timeout.InfiniteTimeSpan);

    [TestMethod]
    public void ZeroStatefulReconnectTimeoutIsRejectedWhenReconnectIsEnabled() =>
        AssertStatefulReconnectTimeoutRejected(TimeSpan.Zero);

    [TestMethod]
    public void NegativeStatefulReconnectTimeoutIsRejectedWhenReconnectIsEnabled() =>
        AssertStatefulReconnectTimeoutRejected(TimeSpan.FromTicks(-1));

    [TestMethod]
    public void StatefulReconnectTimeoutAboveTimerMaximumIsRejectedWhenReconnectIsEnabled() =>
        AssertStatefulReconnectTimeoutRejected(TimeSpan.FromMilliseconds(uint.MaxValue));

    [TestMethod]
    public async Task InvalidStatefulReconnectTimeoutIsIgnoredWhenActivationIsDisabled()
    {
        using var physical = CreatePhysicalConnection("initial");

        var context = CreateContext(physical.Connection, reconnectEnabled: false, timeout: Timeout.InfiniteTimeSpan);

        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StatefulReconnectCanBeEnabledAfterInitialActivation()
    {
        using var physical = CreatePhysicalConnection("initial");
        var context = CreateContext(physical.Connection, reconnectEnabled: false);

        Assert.IsFalse(context.IsReconnectEnabled);
        Assert.IsTrue(context.Features.Get<IRaidoStatefulReconnectFeature>()!.TryEnable());
        Assert.IsTrue(context.IsReconnectEnabled);

        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ReconnectHandoffPreservesTargetIdentityAndFlushesResponseBeforeTransfer()
    {
        using var initial = CreatePhysicalConnection("target");
        using var candidatePhysical = CreatePhysicalConnection("candidate");
        var target = CreateContext(initial.Connection, reconnectEnabled: true);
        var candidate = CreateContext(candidatePhysical.Connection, reconnectEnabled: false);

        var oldProtocolLifetime = new TrackingAsyncDisposable();
        await target.SetProtocolAsync(new PhysicalConnectionWritingProtocol(), oldProtocolLifetime, CancellationToken.None);
        target.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var replacementProtocol = new PhysicalConnectionWritingProtocol();
        var replacementProtocolLifetime = new TrackingAsyncDisposable();
        var result = await target.TryReconnectAsync(
            candidate,
            new byte[] { 15, 0, 4, 1, 2, 3, 4 },
            replacementProtocol,
            replacementProtocolLifetime,
            candidate.ConnectionAborted);

        Assert.IsTrue(result);
        Assert.AreEqual("target", target.ConnectionId);
        Assert.IsTrue(target.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(candidatePhysical.Connection, current);
        Assert.AreEqual("candidate", current.ConnectionId);
        Assert.IsTrue(candidate.TcpConnection.IsTerminal);
        Assert.IsFalse(candidatePhysical.Connection.ConnectionClosed.IsCancellationRequested);
        Assert.AreEqual(1, oldProtocolLifetime.DisposeCount);
        Assert.AreEqual(0, replacementProtocolLifetime.DisposeCount);

        var response = await candidatePhysical.Output.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 15, 0, 4, 1, 2, 3, 4 }, response.Buffer.ToArray());
        candidatePhysical.Output.Reader.AdvanceTo(response.Buffer.End);

        target.TcpConnection.AcknowledgeInputBoundary();
        await candidatePhysical.Input.Writer.WriteAsync(new byte[] { 99 });
        var resumedInput = await ReadNonCanceledAsync(target.TransportInput);
        CollectionAssert.AreEqual(new byte[] { 99 }, resumedInput.Buffer.ToArray());
        target.TransportInput.AdvanceTo(resumedInput.Buffer.End);

        await target.CleanupAsync();
        Assert.AreEqual(1, replacementProtocolLifetime.DisposeCount);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ConcurrentReconnectCandidatesHaveOneWinner()
    {
        using var initial = CreatePhysicalConnection("target");
        using var firstPhysical = CreatePhysicalConnection("first");
        using var secondPhysical = CreatePhysicalConnection("second");
        var target = CreateContext(initial.Connection, reconnectEnabled: true);
        var first = CreateContext(firstPhysical.Connection, reconnectEnabled: false);
        var second = CreateContext(secondPhysical.Connection, reconnectEnabled: false);
        target.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var attempts = await Task.WhenAll(
            target.TryReconnectAsync(first, new byte[] { 1 }, new PhysicalConnectionWritingProtocol(), new NoopAsyncDisposable()).AsTask(),
            target.TryReconnectAsync(second, new byte[] { 2 }, new PhysicalConnectionWritingProtocol(), new NoopAsyncDisposable()).AsTask());

        Assert.AreEqual(1, attempts.Count(result => result));
        Assert.IsTrue(target.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.IsTrue(ReferenceEquals(current, firstPhysical.Connection) || ReferenceEquals(current, secondPhysical.Connection));
    }

    [TestMethod]
    public async Task CandidateCancellationBeforeTransferLeavesTargetUntouched()
    {
        using var initial = CreatePhysicalConnection("target");
        using var candidatePhysical = CreatePhysicalConnection("candidate");
        var target = CreateContext(initial.Connection, reconnectEnabled: true);
        var candidate = CreateContext(candidatePhysical.Connection, reconnectEnabled: false);
        target.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        candidate.Abort();

        var result = await target.TryReconnectAsync(
            candidate,
            new byte[] { 15 },
            new PhysicalConnectionWritingProtocol(),
            new NoopAsyncDisposable(),
            candidate.ConnectionAborted);

        Assert.IsFalse(result);
        Assert.IsFalse(target.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(target.IsReconnectEnabled);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ReconnectImmediatePacketUsesTheFreshProtocolThroughTheHandler()
    {
        using var initial = CreatePhysicalConnection("target");
        using var candidatePhysical = CreatePhysicalConnection("candidate");
        var target = CreateContext(initial.Connection, reconnectEnabled: true);
        var candidate = CreateContext(candidatePhysical.Connection, reconnectEnabled: false);
        var freshMessage = new TestMessage();
        var freshProtocol = new TestProtocol { MessageToReturn = freshMessage };
        await target.SetProtocolAsync(new TestProtocol { ParseMessageReturns = false }, CancellationToken.None);

        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        using var meter = new Meter("Raido.Server.Tests.Reconnect");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            new RaidoMetrics(meterFactory));
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(target, freshMessage).Returns(_ =>
        {
            dispatched.TrySetResult();
            return Task.CompletedTask;
        });

        var run = handler.ConnectAsync(target);
        initial.Closed.Cancel();
        await WaitForConditionAsync(() => !target.TcpConnection.TryGetCurrentConnection(out _));

        var result = await target.TryReconnectAsync(
            candidate,
            new byte[] { 15, 0, 4, 1, 2, 3, 4 },
            freshProtocol,
            new NoopAsyncDisposable(),
            candidate.ConnectionAborted);

        Assert.IsTrue(result);
        var response = await candidatePhysical.Output.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 15, 0, 4, 1, 2, 3, 4 }, response.Buffer.ToArray());
        candidatePhysical.Output.Reader.AdvanceTo(response.Buffer.End);

        await candidatePhysical.Input.Writer.WriteAsync(new byte[] { 1 });
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await dispatcher.Received(1).DispatchMessageAsync(target, freshMessage);

        target.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task PreSignalledConnectionClosedStartsDetachedReconnectWindow()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        initial.Closed.Cancel();

        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PreSignalledConnectionClosedRetainsDetachedPhysicalCloseRequestRegistration()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        initial.Closed.Cancel();

        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);

        closeRequested.RequestClose();

        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        Assert.IsTrue(context.TcpConnection.IsTerminal);

        await context.AbortAsync();

        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task PreSignalledPhysicalConnectionClosedRequestedIsTerminal()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        closeRequested.RequestClose();

        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task PreSignalledPhysicalConnectionClosedAndCloseRequestedAreTerminal()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        initial.Closed.Cancel();
        closeRequested.RequestClose();

        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ProtocolWriteIOExceptionIsTerminalWhenActivationEnabled()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var failure = new IOException("encoder failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new FailingOutputProtocol(writeException: failure), CancellationToken.None);

        await context.WriteAsync(new TestMessage());

        Assert.AreSame(failure, context.TcpConnection.TerminalException);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ProtocolWriteProgrammingFailureIsTerminalWhenActivationEnabled()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var failure = new InvalidOperationException("encoder failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new FailingOutputProtocol(writeException: failure), CancellationToken.None);

        await context.WriteAsync(new TestMessage());

        Assert.AreSame(failure, context.TcpConnection.TerminalException);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task CallerCancelledWriteDoesNotOpenReconnectWindow()
    {
        using var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => context.WriteAsync(new TestMessage(), cancellation.Token).AsTask());

        Assert.IsNull(context.TcpConnection.TerminalException);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(initial.Connection, current);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task CancelledOutboundWriteDoesNotCommitBytesForNextWrite()
    {
        var outputWriter = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: outputWriter);
        var protocol = new PayloadWritingProtocol();
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        protocol.Payload = [17];
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => context.WriteAsync(new TestMessage(), cancellation.Token).AsTask());

        protocol.Payload = [34];
        await context.WriteAsync(new TestMessage());

        var bytes = await outputWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 34 }, bytes);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalDetachBeforeStableCommitDoesNotReplayAdmittedBytes()
    {
        var initialWriter = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: initialWriter);
        var replacementWriter = new RecordingPipeWriter();
        using var replacement = CreatePhysicalConnection("replacement", outputWriter: replacementWriter);
        var protocol = new PayloadWritingProtocol { Payload = [51] };
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        await context.WriteAsync(new TestMessage());
        await initialWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));

        using var cancellation = new CancellationTokenSource();
        using var detachRegistration = cancellation.Token.Register(
            () => context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection));
        protocol.AfterWrite = () =>
        {
            cancellation.Cancel();
        };

        try
        {
            await context.WriteAsync(new TestMessage(), cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // The pre-fix path reports cancellation after leaving the bytes advanced but uncommitted.
        }

        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        protocol.Payload = [68];
        protocol.AfterWrite = null;
        await context.WriteAsync(new TestMessage());

        var bytes = await replacementWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 68 }, bytes);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task HandlerContinuesReadingAfterPhysicalReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var message = new TestMessage();
        await context.SetProtocolAsync(new TestProtocol { MessageToReturn = message }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, message).Returns(_ =>
        {
            dispatched.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.PhysicalConnection");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        initial.Input.Writer.Complete();
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(2));

        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task DetachedPartialMessageIsNotCombinedWithReplacementInput()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var initialMessage = new TestMessage();
        var replacementMessage = new TestMessage();
        var protocol = new StreamBoundaryProtocol(initialMessage, replacementMessage);
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new List<RaidoMessage>();
        var replacementDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, Arg.Any<RaidoMessage>()).Returns(callInfo =>
        {
            var message = callInfo.Arg<RaidoMessage>()!;
            dispatched.Add(message);
            if (ReferenceEquals(message, replacementMessage))
            {
                replacementDispatched.TrySetResult();
            }

            return Task.CompletedTask;
        });
        using var meter = new Meter("Raido.Server.Tests.StreamBoundary");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);

        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await protocol.PartialMessageObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(0, dispatched.Count);

        initial.Closed.Cancel();
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await replacement.Input.Writer.WriteAsync(new byte[] { 2 });
        await protocol.ReplacementInputObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await replacement.Input.Writer.WriteAsync(new byte[] { 3, 4 });
        await replacementDispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(1, dispatched.Count);
        Assert.AreSame(replacementMessage, dispatched[0]);
        Assert.IsFalse(dispatched.Contains(initialMessage));

        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(1));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task PhysicalCloseDetachesReplacementWhilePriorInputBoundaryIsPending()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var inputReader = new PendingPipeReader();
        using var replacement = CreatePhysicalConnection("replacement", inputReader: inputReader);
        using var later = CreatePhysicalConnection("later");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        replacement.Closed.Cancel();

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.IsTerminal);
        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(later.Connection));

        context.TcpConnection.AcknowledgeInputBoundary();
        await later.Input.Writer.WriteAsync(new byte[] { 3, 4 });
        var read = await ReadNonCanceledAsync(context.TcpConnection.Transport.Input);
        CollectionAssert.AreEqual(new byte[] { 3, 4 }, read.Buffer.ToArray());
        context.TcpConnection.Transport.Input.AdvanceTo(read.Buffer.End);

        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task PhysicalCloseIsTerminalWhenReconnectIsDisabledWithoutInputRelay()
    {
        using var inputReader = new PendingPipeReader();
        using var initial = CreatePhysicalConnection("initial", inputReader: inputReader);
        var context = CreateContext(initial.Connection, reconnectEnabled: false);

        initial.Closed.Cancel();

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        await context.AbortAsync();
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StalePhysicalCloseCannotDetachReplacementAndDuplicateCloseIsIdempotent()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        initial.Closed.Cancel();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsFalse(context.TcpConnection.IsTerminal);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalCancellationBeforeStableInputCommitDoesNotContaminateReplacement()
    {
        var initialReader = new CancelBeforeReturningPipeReader(new ReadResult(
            new ReadOnlySequence<byte>(new byte[] { 1 }),
            isCanceled: false,
            isCompleted: false));
        using var initial = CreatePhysicalConnection("initial", inputReader: initialReader);
        using var replacement = CreatePhysicalConnection("replacement");
        var initialMessage = new TestMessage();
        var replacementMessage = new TestMessage();
        var protocol = new StreamBoundaryProtocol(initialMessage, replacementMessage);
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new List<RaidoMessage>();
        var replacementDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, Arg.Any<RaidoMessage>()).Returns(callInfo =>
        {
            var message = callInfo.Arg<RaidoMessage>()!;
            dispatched.Add(message);
            if (ReferenceEquals(message, replacementMessage))
            {
                replacementDispatched.TrySetResult();
            }

            return Task.CompletedTask;
        });
        using var meter = new Meter("Raido.Server.Tests.InputCommitBoundary");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        initialReader.BeforeReturning = initial.Closed.Cancel;
        var run = handler.ConnectAsync(context);
        await initialReader.ReadReturned.Task.WaitAsync(TimeSpan.FromSeconds(1));

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await replacement.Input.Writer.WriteAsync(new byte[] { 2 });
        await protocol.ReplacementInputObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(0, dispatched.Count);

        await replacement.Input.Writer.WriteAsync(new byte[] { 3, 4 });
        await replacementDispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, dispatched.Count);
        Assert.AreSame(replacementMessage, dispatched[0]);
        Assert.IsFalse(dispatched.Contains(initialMessage));
        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(1));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalReadAfterDetachIsDroppedBeforeStableAdmission()
    {
        var inputReader = new CancelBeforeReturningPipeReader(new ReadResult(
            new ReadOnlySequence<byte>(new byte[] { 1 }),
            isCanceled: false,
            isCompleted: false));
        using var initial = CreatePhysicalConnection("initial", inputReader: inputReader);
        using var replacement = CreatePhysicalConnection("replacement");
        var initialMessage = new TestMessage();
        var replacementMessage = new TestMessage();
        var protocol = new StreamBoundaryProtocol(initialMessage, replacementMessage);
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new List<RaidoMessage>();
        var replacementDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, Arg.Any<RaidoMessage>()).Returns(callInfo =>
        {
            var message = callInfo.Arg<RaidoMessage>()!;
            dispatched.Add(message);
            if (ReferenceEquals(message, replacementMessage))
            {
                replacementDispatched.TrySetResult();
            }

            return Task.CompletedTask;
        });
        using var meter = new Meter("Raido.Server.Tests.InputAdmissionBoundary");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        Task? run = null;
        try
        {
            inputReader.BeforeReturning = () => context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
            run = handler.ConnectAsync(context);
            await inputReader.ReadReturned.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.AreEqual(0, dispatched.Count);

            Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
            Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
            context.TcpConnection.AcknowledgeInputBoundary();

            await replacement.Input.Writer.WriteAsync(new byte[] { 2 });
            await protocol.ReplacementInputObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.AreEqual(0, dispatched.Count);

            await replacement.Input.Writer.WriteAsync(new byte[] { 3, 4 });
            await replacementDispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(1, dispatched.Count);
            Assert.AreSame(replacementMessage, dispatched[0]);
            Assert.IsFalse(dispatched.Contains(initialMessage));
        }
        finally
        {
            context.Abort();
            if (run is not null)
            {
                await run.WaitAsync(TimeSpan.FromSeconds(1));
            }

            await context.CleanupAsync();
        }
    }

    [TestMethod]
    public async Task CompleteMessageAndIncompleteTailAreSeparatedAtPhysicalReplacement()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var initialMessage = new TestMessage();
        var replacementMessage = new TestMessage();
        var combinedMessage = new TestMessage();
        var protocol = new CompleteAndPartialStreamBoundaryProtocol(initialMessage, replacementMessage, combinedMessage);
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new List<RaidoMessage>();
        var initialDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var replacementDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, Arg.Any<RaidoMessage>()).Returns(callInfo =>
        {
            var message = callInfo.Arg<RaidoMessage>()!;
            dispatched.Add(message);
            if (ReferenceEquals(message, initialMessage))
            {
                initialDispatched.TrySetResult();
            }

            if (ReferenceEquals(message, replacementMessage))
            {
                replacementDispatched.TrySetResult();
            }

            return Task.CompletedTask;
        });
        using var meter = new Meter("Raido.Server.Tests.CompleteAndPartialBoundary");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1, 2 });
        await initialDispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await initial.Input.Writer.WriteAsync(new byte[] { 9 });
        await protocol.PartialMessageObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, dispatched.Count);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await replacement.Input.Writer.WriteAsync(new byte[] { 10 });
        await protocol.ReplacementInputObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await replacement.Input.Writer.WriteAsync(new byte[] { 3, 4 });
        await replacementDispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(2, dispatched.Count);
        Assert.IsFalse(dispatched.Contains(combinedMessage));
        Assert.AreSame(replacementMessage, dispatched[1]);

        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(1));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalInputCompletionDetachesWithoutPhysicalClosedCallback()
    {
        using var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.PhysicalInputCompletion");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        initial.Input.Writer.Complete();

        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));

        Assert.IsFalse(initial.Closed.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);

        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(2));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task HandlerWaitsWhenItStartsWithDetachedReconnectWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var message = new TestMessage();
        await context.SetProtocolAsync(new TestProtocol { MessageToReturn = message }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, message).Returns(_ =>
        {
            dispatched.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.DetachedHandler");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var run = handler.ConnectAsync(context);

        Assert.IsFalse(run.IsCompleted);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(2));

        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task TimeoutWinsWhilePhysicalHeartbeatIsRegistering()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        var heartbeat = TrackBlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var activation = Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(candidate.Connection));
        await heartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsFalse(await context.TcpConnection.WaitForReconnectAsync(TimeSpan.Zero));
        heartbeat.Release.TrySetResult();
        Assert.IsFalse(await activation.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(candidate.Closed.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(candidate.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ConcurrentPhysicalCandidatesHaveExactlyOnePublishedWinner()
    {
        var initial = CreatePhysicalConnection("initial");
        var first = CreatePhysicalConnection("first");
        var second = CreatePhysicalConnection("second");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var results = await Task.WhenAll(
            Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(first.Connection)),
            Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(second.Connection)));

        Assert.AreEqual(1, results.Count(result => result));
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.IsTrue(ReferenceEquals(current, first.Connection) || ReferenceEquals(current, second.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LatePhysicalCandidateFromTheFirstWindowCannotActivateInTheSecondWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var lateCandidate = CreatePhysicalConnection("late");
        var winner = CreatePhysicalConnection("winner");
        var next = CreatePhysicalConnection("next");
        var blockingHeartbeat = TrackBlockingHeartbeatFeature();
        lateCandidate.Connection.Features.Set<IConnectionHeartbeatFeature>(blockingHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var lateActivation = Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(lateCandidate.Connection));
        await blockingHeartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(winner.Connection));
        context.TcpConnection.OnPhysicalConnectionClosed(winner.Connection);

        blockingHeartbeat.Release.TrySetResult();
        Assert.IsFalse(await lateActivation.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(next.Connection));
        await context.CleanupAsync();
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
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var activation = Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        var exception = new IOException("stale flush");
        pendingWriter.Fail(exception);
        await pendingWrite;
        Assert.IsTrue(await activation.WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task DisabledConnectionTerminatesImmediatelyOnPhysicalLoss()
    {
        var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: false);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task DetachedConnectionRetainsStableTransportPipes()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableTransport = context.TcpConnection.Transport;

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsNull(context.LocalEndPoint);
        Assert.IsNull(context.RemoteEndPoint);
        Assert.AreSame(stableTransport, context.TcpConnection.Transport);
        Assert.IsNotNull(context.TcpConnection.Transport.Input);
        Assert.IsNotNull(context.TcpConnection.Transport.Output);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ExplicitAbortWhileDetachedCompletesTheReconnectWindowAndStaysTerminal()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var physicalConnectionWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        context.Abort();

        Assert.IsFalse(await physicalConnectionWindow.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task AlreadyClosedPhysicalCandidateIsRejectedWithoutPublishingATransport()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        using var closeRequested = new CloseRequestedFeature();
        candidate.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        closeRequested.RequestClose();
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(candidate.Connection));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(closeRequested.ConnectionClosedRequested.IsCancellationRequested);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task RejectedPhysicalCandidateRemainsOwnedByTheCaller()
    {
        var initial = CreatePhysicalConnection("initial");
        var winner = CreatePhysicalConnection("winner");
        var rejected = CreatePhysicalConnection("rejected");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(winner.Connection));

        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(rejected.Connection));
        Assert.IsFalse(rejected.Closed.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(winner.Connection, current);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task CurrentWriteFailureDetachesAndAllowsAReplacement()
    {
        var pendingWriter = new DeferredFailingPipeWriter();
        var initial = CreatePhysicalConnection("initial", outputWriter: pendingWriter);
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var pendingWrite = context.WriteAsync(new TestMessage());
        var exception = new IOException("current flush");
        pendingWriter.Fail(exception);
        await pendingWrite;
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));

        Assert.IsNull(context.TcpConnection.TerminalException);

        var physicalConnectionWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        Assert.IsTrue(await physicalConnectionWindow);
        Assert.IsNull(context.TcpConnection.TerminalException);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task FailedPhysicalConnectionExceptionIsClearedBySuccessfulReplacement()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var failure = new IOException("detached transport failure");

        Assert.IsTrue(context.TcpConnection.HandleTransportFailure(initial.Connection, failure));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        context.TcpConnection.OnPhysicalConnectionClosed(replacement.Connection);
        Assert.IsFalse(await context.TcpConnection.WaitForReconnectAsync(TimeSpan.Zero));
        Assert.IsNull(context.TcpConnection.TerminalException);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task FailedPhysicalConnectionExceptionBecomesTerminalAfterReconnectTimeout()
    {
        using var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var failure = new IOException("detached transport failure");

        Assert.IsTrue(context.TcpConnection.HandleTransportFailure(initial.Connection, failure));
        Assert.IsFalse(await context.TcpConnection.WaitForReconnectAsync(TimeSpan.Zero));

        Assert.AreSame(failure, context.TcpConnection.TerminalException);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StaleTransportFailureCannotDetachTheReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        var exception = new IOException("stale read");
        Assert.IsTrue(context.TcpConnection.HandleTransportFailure(initial.Connection, exception));

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsNull(context.TcpConnection.TerminalException);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task CurrentReadFailureDetachesAndHandlerContinuesWithTheReplacement()
    {
        var readFailure = new IOException("current read");
        var failingReader = new ThrowingPipeReader(readFailure);
        var initial = CreatePhysicalConnection("initial", inputReader: failingReader);
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var message = new TestMessage();
        await context.SetProtocolAsync(new TestProtocol { MessageToReturn = message }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, message).Returns(_ =>
        {
            dispatched.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ReadFailure");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);

        await failingReader.ReadInvoked.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        replacement.Input.Writer.Complete();
        context.Abort();

        await run.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsNull(context.TcpConnection.TerminalException);
        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task CurrentReadCancellationDetachesAndAllowsAReplacement()
    {
        var readCancellation = new OperationCanceledException("current read");
        var failingReader = new ThrowingPipeReader(readCancellation);
        var initial = CreatePhysicalConnection("initial", inputReader: failingReader);
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ReadCancellation");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            Substitute.For<IRaidoDispatcher>(),
            metrics);

        var run = handler.ConnectAsync(context);
        await failingReader.ReadInvoked.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        context.Abort();
        await context.AbortAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task UnexpectedPhysicalInputRelayFaultTerminatesLogicalConnection()
    {
        var relayFailure = new InvalidOperationException("physical input relay failed");
        var transportAccessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var physicalTransport = Substitute.For<IDuplexPipe>();
        physicalTransport.Input.Returns(Substitute.For<PipeReader>());
        physicalTransport.Output.Returns(Substitute.For<PipeWriter>());
        var transportAccessCount = 0;
        var physicalConnection = Substitute.For<ConnectionContext>();
        physicalConnection.ConnectionId.Returns("faulting");
        physicalConnection.Features.Returns(new FeatureCollection());
        physicalConnection.Items.Returns(new Dictionary<object, object?>());
        physicalConnection.LocalEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, 1000));
        physicalConnection.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Loopback, 2000));
        physicalConnection.ConnectionClosed.Returns(CancellationToken.None);
        physicalConnection.Transport.Returns(_ =>
        {
            if (Interlocked.Increment(ref transportAccessCount) == 1)
            {
                transportAccessed.TrySetResult();
                throw relayFailure;
            }

            return physicalTransport;
        });
        var context = CreateContext(physicalConnection, reconnectEnabled: true);
        var terminal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var terminalRegistration = context.ConnectionAborted.Register(() => terminal.TrySetResult());
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        var allowHandlerStart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lifetimeManager.OnConnectedAsync(context).Returns(allowHandlerStart.Task);
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.UnexpectedRelayFault");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            Substitute.For<IRaidoDispatcher>(),
            metrics);

        var run = handler.ConnectAsync(context);

        await transportAccessed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await terminal.Task.WaitAsync(TimeSpan.FromSeconds(1));
        allowHandlerStart.TrySetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.AreSame(relayFailure, context.TcpConnection.TerminalException);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalCandidateClosingDuringRegistrationIsRejectedAndLeavesTheWindowOpen()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        var winner = CreatePhysicalConnection("winner");
        var heartbeat = TrackBlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var candidateActivation = Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(candidate.Connection));
        await heartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        candidate.Closed.Cancel();
        heartbeat.Release.TrySetResult();

        Assert.IsFalse(await candidateActivation.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));

        var physicalConnectionWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(winner.Connection));
        Assert.IsTrue(await physicalConnectionWindow);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ClientTimeoutHeartbeatMovesToTheReplacementAndIgnoresTheStaleHeartbeat()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true, clientTimeout: TimeSpan.Zero);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.StartClientTimeout();
        context.BeginClientTimeout();
        context.StopClientTimeout();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        Assert.AreEqual(1, initialHeartbeat.Callbacks.Count);
        Assert.AreEqual(1, replacementHeartbeat.Callbacks.Count);

        replacementHeartbeat.Run();
        initialHeartbeat.Run();

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ReplacementHeartbeatWaitsForThePreviousInputBoundary()
    {
        var clock = new ManualTimeProvider();
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        var context = CreateContext(
            initial.Connection,
            reconnectEnabled: true,
            clientTimeout: TimeSpan.FromSeconds(1),
            timeout: TimeSpan.FromSeconds(5),
            timeProvider: clock);
        context.StartClientTimeout();
        context.BeginClientTimeout();
        clock.Advance(TimeSpan.FromSeconds(2));

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        replacementHeartbeat.Run();
        Assert.IsFalse(context.TcpConnection.IsTerminal);

        context.TcpConnection.AcknowledgeInputBoundary();
        context.StopClientTimeout();
        context.BeginClientTimeout();
        clock.Advance(TimeSpan.FromSeconds(2));
        replacementHeartbeat.Run();
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task InherentKeepAliveSuppressesRaidoPing()
    {
        var clock = new ManualTimeProvider();
        var heartbeat = new RecordingHeartbeatFeature();
        var writer = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: writer);
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        initial.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(true));
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeProvider: clock);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        clock.Advance(TimeSpan.FromMinutes(2));
        heartbeat.Run();

        Assert.IsTrue(context.Features.Get<IConnectionInherentKeepAliveFeature>()!.HasInherentKeepAlive);
        Assert.IsFalse(writer.FirstFlush.Task.IsCompleted);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task MissingInherentKeepAliveSendsRaidoPing()
    {
        var clock = new ManualTimeProvider();
        var heartbeat = new RecordingHeartbeatFeature();
        var writer = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: writer);
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        initial.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(false));
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeProvider: clock);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        clock.Advance(TimeSpan.FromMinutes(2));
        heartbeat.Run();

        var ping = await writer.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 42 }, ping);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ReplacementInherentKeepAliveSuppressesPingAfterInitialPhysicalConnectionDidNotHaveIt()
    {
        var clock = new ManualTimeProvider();
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        var replacementWriter = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement", outputWriter: replacementWriter);
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        initial.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(false));
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        replacement.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(true));
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromMinutes(5), timeProvider: clock);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        context.TcpConnection.AcknowledgeInputBoundary();
        clock.Advance(TimeSpan.FromMinutes(2));
        replacementHeartbeat.Run();

        Assert.IsTrue(context.Features.Get<IConnectionInherentKeepAliveFeature>()!.HasInherentKeepAlive);
        Assert.IsFalse(replacementWriter.FirstFlush.Task.IsCompleted);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ReplacementWithoutInherentKeepAliveResumesPingAfterInitialPhysicalConnectionHadIt()
    {
        var clock = new ManualTimeProvider();
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        var replacementWriter = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement", outputWriter: replacementWriter);
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        initial.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(true));
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        replacement.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(false));
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromMinutes(5), timeProvider: clock);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        context.TcpConnection.AcknowledgeInputBoundary();
        clock.Advance(TimeSpan.FromMinutes(2));
        replacementHeartbeat.Run();

        Assert.IsFalse(context.Features.Get<IConnectionInherentKeepAliveFeature>()!.HasInherentKeepAlive);
        var ping = await replacementWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 42 }, ping);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task DetachedKeepAliveHeartbeatDoesNotSendRaidoPing()
    {
        var clock = new ManualTimeProvider();
        var heartbeat = new RecordingHeartbeatFeature();
        var writer = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: writer);
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        initial.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(false));
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeProvider: clock);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        clock.Advance(TimeSpan.FromMinutes(2));
        heartbeat.Run();

        Assert.IsFalse(writer.FirstFlush.Task.IsCompleted);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StalePhysicalKeepAliveHeartbeatDoesNotSendPingAfterReplacement()
    {
        var clock = new ManualTimeProvider();
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        var replacementWriter = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement", outputWriter: replacementWriter);
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        initial.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(false));
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        replacement.Connection.Features.Set<IConnectionInherentKeepAliveFeature>(new InherentKeepAliveFeature(false));
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromMinutes(5), timeProvider: clock);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        context.TcpConnection.AcknowledgeInputBoundary();
        clock.Advance(TimeSpan.FromMinutes(2));

        initialHeartbeat.Run();
        Assert.IsFalse(replacementWriter.FirstFlush.Task.IsCompleted);
        replacementHeartbeat.Run();

        var ping = await replacementWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 42 }, ping);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RapidPhysicalReplacementDoesNotOrphanTheInputBoundaryWaiter()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var later = CreatePhysicalConnection("later");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        Task? run = null;
        try
        {
            context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
            Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
            var firstBoundary = context.TcpConnection.WaitForInputBoundaryAsync();

            context.TcpConnection.OnPhysicalConnectionClosed(replacement.Connection);
            context.TcpConnection.AcknowledgeInputBoundary();

            Assert.IsTrue(await firstBoundary.WaitAsync(TimeSpan.FromSeconds(1)));
            Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(later.Connection));

            var message = new TestMessage();
            await context.SetProtocolAsync(new TestProtocol { MessageToReturn = message }, CancellationToken.None);
            var dispatcher = Substitute.For<IRaidoDispatcher>();
            var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            dispatcher.DispatchMessageAsync(context, message).Returns(_ =>
            {
                dispatched.TrySetResult();
                return Task.CompletedTask;
            });
            using var meter = new Meter("Raido.Server.Tests.RapidInputBoundary");
            var meterFactory = Substitute.For<IMeterFactory>();
            meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
            using var metrics = new RaidoMetrics(meterFactory);
            var handler = new RaidoHubConnectionHandler(
                NullLoggerFactory.Instance,
                Options.Create(new RaidoOptions()),
                Substitute.For<IRaidoHubLifetimeManager>(),
                dispatcher,
                metrics);

            run = handler.ConnectAsync(context);
            await later.Input.Writer.WriteAsync(new byte[] { 1 });
            await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));

            await dispatcher.Received(1).DispatchMessageAsync(context, message);
        }
        finally
        {
            context.Abort();
            if (run is not null)
            {
                await run.WaitAsync(TimeSpan.FromSeconds(1));
            }

            await context.CleanupAsync();
        }
    }

    [TestMethod]
    public async Task StalePhysicalHeartbeatDoesNotInvokeStableHeartbeatAfterReplacement()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var ticks = 0;
        context.TcpConnection.OnHeartbeat(_ => ticks++, null!);

        initialHeartbeat.Run();
        Assert.AreEqual(1, ticks);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        initialHeartbeat.Run();
        Assert.AreEqual(1, ticks);
        replacementHeartbeat.Run();
        Assert.AreEqual(1, ticks);
        context.TcpConnection.AcknowledgeInputBoundary();
        replacementHeartbeat.Run();
        Assert.AreEqual(2, ticks);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LosingPhysicalCandidateHeartbeatDoesNotInvokeStableHeartbeat()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var candidate = CreatePhysicalConnection("candidate");
        using var winner = CreatePhysicalConnection("winner");
        var candidateHeartbeat = TrackBlockingHeartbeatFeature();
        var winnerHeartbeat = new RecordingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(candidateHeartbeat);
        winner.Connection.Features.Set<IConnectionHeartbeatFeature>(winnerHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var ticks = 0;
        context.TcpConnection.OnHeartbeat(_ => ticks++, null!);
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var candidateActivation = Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(candidate.Connection));
        await candidateHeartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(winner.Connection));
        candidateHeartbeat.Release.TrySetResult();

        Assert.IsFalse(await candidateActivation.WaitAsync(TimeSpan.FromSeconds(1)));
        candidateHeartbeat.Run();
        Assert.AreEqual(0, ticks);
        context.TcpConnection.AcknowledgeInputBoundary();
        winnerHeartbeat.Run();
        Assert.AreEqual(1, ticks);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableConnectionFeaturesRemainConsistentAcrossReplacement()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var physicalItems = Substitute.For<IConnectionItemsFeature>();
        var physicalId = Substitute.For<IConnectionIdFeature>();
        var physicalTransport = Substitute.For<IConnectionTransportFeature>();
        var physicalLifetime = Substitute.For<IConnectionLifetimeFeature>();
        var physicalMemoryPool = Substitute.For<IMemoryPoolFeature>();
        var physicalEndPoint = Substitute.For<IConnectionEndPointFeature>();
        var physicalSocket = Substitute.For<IConnectionSocketFeature>();
        var physicalMetricsTags = Substitute.For<IConnectionMetricsTagsFeature>();
        var physicalComplete = Substitute.For<IConnectionCompleteFeature>();
        var physicalTlsConnection = Substitute.For<ITlsConnectionFeature>();
        var physicalTlsHandshake = Substitute.For<ITlsHandshakeFeature>();
        var physicalTlsApplicationProtocol = Substitute.For<ITlsApplicationProtocolFeature>();
        var physicalSslStream = Substitute.For<ISslStreamFeature>();
        var physicalUser = Substitute.For<IConnectionUserFeature>();
        var replacementTransport = Substitute.For<IConnectionTransportFeature>();
        var replacementMemoryPool = Substitute.For<IMemoryPoolFeature>();
        var replacementEndPoint = Substitute.For<IConnectionEndPointFeature>();
        var replacementSocket = Substitute.For<IConnectionSocketFeature>();
        var replacementMetricsTags = Substitute.For<IConnectionMetricsTagsFeature>();
        var replacementComplete = Substitute.For<IConnectionCompleteFeature>();
        var replacementTlsConnection = Substitute.For<ITlsConnectionFeature>();
        var replacementTlsHandshake = Substitute.For<ITlsHandshakeFeature>();
        var replacementTlsApplicationProtocol = Substitute.For<ITlsApplicationProtocolFeature>();
        var replacementSslStream = Substitute.For<ISslStreamFeature>();
        var logicalFeature = new object();
        var unknownPhysicalFeature = new UnknownPhysicalFeature();
        initial.Connection.Features.Set(physicalItems);
        initial.Connection.Features.Set(physicalId);
        initial.Connection.Features.Set(physicalTransport);
        initial.Connection.Features.Set(physicalLifetime);
        initial.Connection.Features.Set(physicalMemoryPool);
        initial.Connection.Features.Set(physicalEndPoint);
        initial.Connection.Features.Set(physicalSocket);
        initial.Connection.Features.Set(physicalMetricsTags);
        initial.Connection.Features.Set(physicalComplete);
        initial.Connection.Features.Set(physicalTlsConnection);
        initial.Connection.Features.Set(physicalTlsHandshake);
        initial.Connection.Features.Set(physicalTlsApplicationProtocol);
        initial.Connection.Features.Set(physicalSslStream);
        initial.Connection.Features.Set(physicalUser);
        initial.Connection.Features.Set(unknownPhysicalFeature);
        replacement.Connection.Features.Set(replacementTransport);
        replacement.Connection.Features.Set(replacementMemoryPool);
        replacement.Connection.Features.Set(replacementEndPoint);
        replacement.Connection.Features.Set(replacementSocket);
        replacement.Connection.Features.Set(replacementMetricsTags);
        replacement.Connection.Features.Set(replacementComplete);
        replacement.Connection.Features.Set(replacementTlsConnection);
        replacement.Connection.Features.Set(replacementTlsHandshake);
        replacement.Connection.Features.Set(replacementTlsApplicationProtocol);
        replacement.Connection.Features.Set(replacementSslStream);

        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Features.Set(logicalFeature);
        var stableItems = context.Features.Get<IConnectionItemsFeature>();
        var stableId = context.Features.Get<IConnectionIdFeature>();
        var stableTransport = context.Features.Get<IConnectionTransportFeature>();
        var stableLifetime = context.Features.Get<IConnectionLifetimeFeature>();
        var stableHeartbeat = context.Features.Get<IConnectionHeartbeatFeature>();

        Assert.AreSame(context.Items, stableItems!.Items);
        Assert.AreSame(context.TcpConnection, stableId);
        Assert.AreSame(context.TcpConnection, stableTransport);
        Assert.AreSame(context.TcpConnection, stableLifetime);
        Assert.AreSame(context.TcpConnection, stableHeartbeat);
        Assert.AreNotSame(physicalItems, stableItems);
        Assert.AreNotSame(physicalId, stableId);
        Assert.AreNotSame(physicalTransport, stableTransport);
        Assert.AreNotSame(physicalLifetime, stableLifetime);
        Assert.AreSame(logicalFeature, context.Features.Get<object>());
        Assert.AreSame(physicalUser, context.Features.Get<IConnectionUserFeature>());
        Assert.IsNull(context.Features.Get<IMemoryPoolFeature>());
        Assert.IsNull(context.Features.Get<IConnectionEndPointFeature>());
        Assert.IsNull(context.Features.Get<IConnectionSocketFeature>());
        Assert.IsNull(context.Features.Get<IConnectionMetricsTagsFeature>());
        Assert.IsNull(context.Features.Get<IConnectionCompleteFeature>());
        Assert.IsNull(context.Features.Get<ITlsConnectionFeature>());
        Assert.IsNull(context.Features.Get<ITlsHandshakeFeature>());
        Assert.IsNull(context.Features.Get<ITlsApplicationProtocolFeature>());
        Assert.IsNull(context.Features.Get<ISslStreamFeature>());
        Assert.IsNull(context.Features.Get<UnknownPhysicalFeature>());

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        Assert.AreSame(stableItems, context.Features.Get<IConnectionItemsFeature>());
        Assert.AreSame(stableHeartbeat, context.Features.Get<IConnectionHeartbeatFeature>());
        Assert.AreSame(stableLifetime, context.Features.Get<IConnectionLifetimeFeature>());
        Assert.AreNotSame(replacementTransport, context.Features.Get<IConnectionTransportFeature>());
        Assert.AreSame(logicalFeature, context.Features.Get<object>());
        Assert.AreSame(physicalUser, context.Features.Get<IConnectionUserFeature>());
        Assert.IsNull(context.Features.Get<IMemoryPoolFeature>());
        Assert.IsNull(context.Features.Get<IConnectionEndPointFeature>());
        Assert.IsNull(context.Features.Get<IConnectionSocketFeature>());
        Assert.IsNull(context.Features.Get<IConnectionMetricsTagsFeature>());
        Assert.IsNull(context.Features.Get<IConnectionCompleteFeature>());
        Assert.IsNull(context.Features.Get<ITlsConnectionFeature>());
        Assert.IsNull(context.Features.Get<ITlsHandshakeFeature>());
        Assert.IsNull(context.Features.Get<ITlsApplicationProtocolFeature>());
        Assert.IsNull(context.Features.Get<ISslStreamFeature>());
        Assert.IsNull(context.Features.Get<UnknownPhysicalFeature>());
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableItemsSnapshotInitialPhysicalItemsAndIgnoreReplacementItems()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var initialItems = initial.Connection.Items;
        var initialMiddlewareState = new object();
        var logicalState = new object();
        var replacementMiddlewareState = new object();
        var replacementOnlyState = new object();
        initialItems["middleware-state"] = initialMiddlewareState;

        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableItems = context.Items;
        stableItems["logical-state"] = logicalState;
        replacement.Connection.Items["middleware-state"] = replacementMiddlewareState;
        replacement.Connection.Items["replacement-only"] = replacementOnlyState;

        Assert.AreNotSame(initialItems, stableItems);
        Assert.AreSame(initialMiddlewareState, stableItems["middleware-state"]);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        Assert.AreSame(stableItems, context.Items);
        Assert.AreSame(initialMiddlewareState, stableItems["middleware-state"]);
        Assert.AreSame(logicalState, stableItems["logical-state"]);
        Assert.IsFalse(stableItems.ContainsKey("replacement-only"));

        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableLifetimeNotificationIgnoresStalePhysicalCloseRequestAfterReplacement()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var initialCloseRequested = new CloseRequestedFeature();
        using var replacementCloseRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(initialCloseRequested);
        replacement.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(replacementCloseRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableFeature = context.Features.Get<IConnectionLifetimeNotificationFeature>();
        Assert.AreSame((object)context.TcpConnection, stableFeature);
        Assert.ThrowsExactly<NotSupportedException>(() => stableFeature!.ConnectionClosedRequested = CancellationToken.None);
        var stableRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stableRegistration = stableFeature!.ConnectionClosedRequested.Register(stableRequested.SetResult);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        initialCloseRequested.RequestClose();

        Assert.IsFalse(stableRequested.Task.IsCompleted);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);

        stableFeature.RequestClose();
        Assert.IsFalse(replacementCloseRequested.ConnectionClosedRequested.IsCancellationRequested);
        await stableRequested.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);

        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableLifetimeNotificationReportsAuthoritativeDetachedCloseRequest()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableFeature = context.Features.Get<IConnectionLifetimeNotificationFeature>();
        var stableRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stableRegistration = stableFeature!.ConnectionClosedRequested.Register(stableRequested.SetResult);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        closeRequested.RequestClose();

        await stableRequested.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);

        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableLifetimeNotificationObserverFailureDoesNotPreventTerminalCompletion()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableFeature = context.Features.Get<IConnectionLifetimeNotificationFeature>();
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var connectionClosedRegistration = context.ConnectionAborted.Register(connectionClosed.SetResult);
        using var throwingRegistration = stableFeature!.ConnectionClosedRequested.Register(
            static () => throw new InvalidOperationException("observer failure"));

        Exception? requestException = null;
        try
        {
            closeRequested.RequestClose();
        }
        catch (Exception exception)
        {
            requestException = exception;
        }

        Assert.IsNull(requestException);
        await connectionClosed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        initial.Connection.Received().Abort();

        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task BlockingStableLifetimeNotificationObserverCannotDelayTerminalCompletion()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableFeature = context.Features.Get<IConnectionLifetimeNotificationFeature>();
        var observerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observerRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observerExited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var observerRegistration = stableFeature!.ConnectionClosedRequested.Register(() =>
        {
            observerEntered.TrySetResult();
            try
            {
                observerRelease.Task.GetAwaiter().GetResult();
            }
            finally
            {
                observerExited.TrySetResult();
            }
        });
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var connectionClosedRegistration = context.ConnectionAborted.Register(connectionClosed.SetResult);
        var physicalCloseTask = Task.Run(closeRequested.RequestClose);

        try
        {
            await observerEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsTrue(stableFeature.ConnectionClosedRequested.IsCancellationRequested);
            Assert.IsTrue(context.TcpConnection.IsTerminal);
            await connectionClosed.Task.WaitAsync(TimeSpan.FromSeconds(1));
            initial.Connection.Received().Abort();
            Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
            await context.AbortAsync().WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            observerRelease.TrySetResult();
            await observerExited.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await physicalCloseTask.WaitAsync(TimeSpan.FromSeconds(1));
        }

        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableRequestCloseTerminalizesWithoutPhysicalLifetimeFeature()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableFeature = context.Features.Get<IConnectionLifetimeNotificationFeature>();
        var stableRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stableRegistration = stableFeature!.ConnectionClosedRequested.Register(stableRequested.SetResult);

        stableFeature.RequestClose();

        await stableRequested.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);

        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableRequestCloseOwnsLogicalCloseWithoutPhysicalForwarding()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableFeature = context.Features.Get<IConnectionLifetimeNotificationFeature>();
        var stableRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stableRegistration = stableFeature!.ConnectionClosedRequested.Register(stableRequested.SetResult);

        stableFeature.RequestClose();

        Assert.IsTrue(stableFeature.ConnectionClosedRequested.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        await stableRequested.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.AbortAsync().WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(0, closeRequested.RequestCloseCount);
        await context.CleanupAsync().WaitAsync(TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task ProtocolParserFailureIsTerminalAndCannotOpenReconnectWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var parserFailure = new InvalidOperationException("parser failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new TestProtocol { ParseException = parserFailure }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ParserFailure");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, parserFailure);
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ParserIOExceptionIsTerminalWhenItDoesNotComeFromThePhysicalConnectionRead()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var parserFailure = new IOException("parser failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new TestProtocol { ParseException = parserFailure }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ParserIOException");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, parserFailure);
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ParserCancellationIsTerminalWhenItDoesNotComeFromThePhysicalConnectionRead()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var parserFailure = new OperationCanceledException("parser failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new TestProtocol { ParseException = parserFailure }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ParserCancellation");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, parserFailure);
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task IncompleteProtocolDataIsTerminalAndCannotOpenReconnectWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new TestProtocol { ParseMessageReturns = false }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.IncompleteProtocolData");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await context.TcpConnection.Application.Output.WriteAsync(new byte[] { 1 });
        context.TcpConnection.Application.Output.Complete();
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Is<Exception?>(exception => exception is InvalidDataException));
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task OversizedProtocolDataIsTerminalAndCannotOpenReconnectWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new TestProtocol { ParseMessageReturns = false }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.OversizedProtocolData");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions { MaximumReceiveMessageSize = 0 }),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Is<Exception?>(exception => exception is InvalidDataException));
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ApplicationDispatchFailureIsTerminalAndCannotOpenReconnectWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var message = new TestMessage();
        var dispatchFailure = new InvalidOperationException("dispatch failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new TestProtocol { MessageToReturn = message }, CancellationToken.None);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        dispatcher.DispatchMessageAsync(context, message).Returns(Task.FromException(dispatchFailure));
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ApplicationFailure");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, dispatchFailure);
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task ConnectAsyncKeepsLifetimeCallbacksStableAcrossReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var message = new TestMessage();
        await context.SetProtocolAsync(new TestProtocol { MessageToReturn = message }, CancellationToken.None);
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.DispatchMessageAsync(context, message).Returns(_ =>
        {
            dispatched.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.PhysicalConnectionLifetime");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        initial.Input.Writer.Complete();
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        replacement.Input.Writer.Complete();
        context.Abort();

        await run.WaitAsync(TimeSpan.FromSeconds(2));

        await lifetimeManager.Received(1).OnConnectedAsync(context);
        await lifetimeManager.Received(1).OnDisconnectedAsync(context);
        await dispatcher.Received(1).OnConnectedAsync(context);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
        await dispatcher.Received(1).DispatchMessageAsync(context, message);
    }

    [TestMethod]
    public async Task TcpReplacementDoesNotChangeHubProtocol()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var originalProtocol = context.Protocol;

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var physicalConnectionWaiter = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        Assert.AreSame(originalProtocol, context.Protocol);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsTrue(await physicalConnectionWaiter);

        await context.CleanupAsync();
    }

    private sealed class FailingOutputProtocol(Exception? writeException = null, Exception? messageBytesException = null) : IRaidoProtocol
    {
        public string Name => "failing-output";

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
            if (writeException is not null)
            {
                throw writeException;
            }
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
        {
            if (messageBytesException is not null)
            {
                throw messageBytesException;
            }

            return new byte[] { 42 };
        }

        public bool IsVersionSupported(int version) => version == Version;
    }

    private sealed class PhysicalConnectionWritingProtocol : IRaidoProtocol
    {
        private static readonly byte[] Payload = [42];

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
            Payload.CopyTo(output.GetSpan(Payload.Length));
            output.Advance(Payload.Length);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => Payload;

        public bool IsVersionSupported(int version) => version == 1;
    }

    private sealed class PayloadWritingProtocol : IRaidoProtocol
    {
        public byte[] Payload { get; set; } = [];

        public Action? AfterWrite { get; set; }

        public string Name => "payload-writing";

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
            Payload.CopyTo(output.GetSpan(Payload.Length));
            output.Advance(Payload.Length);
            AfterWrite?.Invoke();
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => Payload;

        public bool IsVersionSupported(int version) => version == Version;
    }

    private sealed class BlockingWriteProtocol : IRaidoProtocol
    {
        public TaskCompletionSource WriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Name => "blocking-write";

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
            WriteStarted.TrySetResult();
            Release.Task.GetAwaiter().GetResult();
            output.GetSpan(1)[0] = 42;
            output.Advance(1);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => new byte[] { 42 };

        public bool IsVersionSupported(int version) => version == 1;
    }

    private sealed class StreamBoundaryProtocol(TestMessage initialMessage, TestMessage replacementMessage) : IRaidoProtocol
    {
        public string Name => "stream-boundary";

        public int Version => 1;

        public TaskCompletionSource PartialMessageObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReplacementInputObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out RaidoMessage message)
        {
            var bytes = input.ToArray();
            if (bytes.Length >= 2 && bytes[0] == 1 && bytes[1] == 2)
            {
                ReplacementInputObserved.TrySetResult();
                consumed = input.GetPosition(2);
                examined = consumed;
                message = initialMessage;
                return true;
            }

            if (bytes.Length >= 2 && bytes[0] == 3 && bytes[1] == 4)
            {
                consumed = input.GetPosition(2);
                examined = consumed;
                message = replacementMessage;
                return true;
            }

            if (bytes.Length > 0 && bytes[0] == 2)
            {
                ReplacementInputObserved.TrySetResult();
                consumed = input.GetPosition(1);
                examined = input.End;
                message = null!;
                return false;
            }

            if (bytes.Length == 1 && bytes[0] == 1)
            {
                PartialMessageObserved.TrySetResult();
            }

            consumed = input.Start;
            examined = input.End;
            message = null!;
            return false;
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output) { }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => ReadOnlyMemory<byte>.Empty;

        public bool IsVersionSupported(int version) => version == Version;
    }

    private sealed class CompleteAndPartialStreamBoundaryProtocol(
        TestMessage initialMessage,
        TestMessage replacementMessage,
        TestMessage combinedMessage) : IRaidoProtocol
    {
        public string Name => "complete-and-partial-stream-boundary";

        public int Version => 1;

        public TaskCompletionSource PartialMessageObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReplacementInputObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out RaidoMessage message)
        {
            var bytes = input.ToArray();
            if (bytes.Length >= 2 && bytes[0] == 1 && bytes[1] == 2)
            {
                consumed = input.GetPosition(2);
                examined = consumed;
                message = initialMessage;
                return true;
            }

            if (bytes.Length >= 2 && bytes[0] == 9 && bytes[1] == 10)
            {
                consumed = input.GetPosition(2);
                examined = consumed;
                message = combinedMessage;
                return true;
            }

            if (bytes.Length >= 2 && bytes[0] == 3 && bytes[1] == 4)
            {
                consumed = input.GetPosition(2);
                examined = consumed;
                message = replacementMessage;
                return true;
            }

            if (bytes.Length == 1 && bytes[0] == 9)
            {
                PartialMessageObserved.TrySetResult();
                consumed = input.Start;
                examined = input.End;
                message = null!;
                return false;
            }

            if (bytes.Length > 0 && bytes[0] == 10)
            {
                ReplacementInputObserved.TrySetResult();
                consumed = input.GetPosition(1);
                examined = input.End;
                message = null!;
                return false;
            }

            consumed = input.Start;
            examined = input.End;
            message = null!;
            return false;
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output) { }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => ReadOnlyMemory<byte>.Empty;

        public bool IsVersionSupported(int version) => version == Version;
    }

    [TestMethod]
    public async Task DetachedReadIsWokenWithoutCancellingTheStableAbortToken()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var pendingRead = context.TcpConnection.Transport.Input.ReadAsync().AsTask();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var result = await pendingRead.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        context.TcpConnection.Transport.Input.AdvanceTo(result.Buffer.End);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task DetachedWriteDoesNotTouchAProductionPipe()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        await context.WriteAsync(new TestMessage());

        Assert.IsFalse(initial.Output.Reader.TryRead(out _));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task DetachedWriteBehindBlockedPhysicalFlushIsDroppedBeforeReplacement()
    {
        var blockedWriter = TrackBlockingPipeWriter();
        var replacementWriter = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: blockedWriter);
        using var replacement = CreatePhysicalConnection("replacement", outputWriter: replacementWriter);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var firstWrite = context.WriteAsync(new TestMessage());
        await blockedWriter.FlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        await context.WriteAsync(new TestMessage());
        Assert.IsFalse(replacementWriter.FirstFlush.Task.IsCompleted);

        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        blockedWriter.Release();
        await firstWrite;

        await context.WriteAsync(new TestMessage());
        var replacementBytes = await replacementWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));

        CollectionAssert.AreEqual(new byte[] { 42 }, replacementBytes);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task DetachedWriteIsDroppedBeforeReplacementPublication()
    {
        using var initial = CreatePhysicalConnection("initial");
        var replacementWriter = new RecordingPipeWriter();
        using var replacement = CreatePhysicalConnection("replacement", outputWriter: replacementWriter);
        var protocol = new BlockingWriteProtocol();
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        Task? writeTask = null;

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        try
        {
            writeTask = Task.Run(() => context.WriteAsync(new TestMessage()).AsTask());
            await Task.WhenAny(writeTask, protocol.WriteStarted.Task).WaitAsync(TimeSpan.FromSeconds(1));
            var writeStarted = protocol.WriteStarted.Task.IsCompleted;

            Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
            protocol.Release.TrySetResult();
            await writeTask.WaitAsync(TimeSpan.FromSeconds(1));

            if (writeStarted)
            {
                await replacementWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));
            }

            Assert.IsFalse(writeStarted);
            Assert.IsFalse(replacementWriter.FirstFlush.Task.IsCompleted);
        }
        finally
        {
            protocol.Release.TrySetResult();
            if (writeTask is not null)
            {
                await writeTask.WaitAsync(TimeSpan.FromSeconds(1));
            }

            await context.CleanupAsync();
        }
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalDetachWaitsForActiveProtocolSerialization()
    {
        using var initial = CreatePhysicalConnection("initial");
        var protocol = new BlockingWriteProtocol();
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        Task? writeTask = null;
        Task? detachTask = null;

        try
        {
            writeTask = Task.Run(() => context.WriteAsync(new TestMessage()).AsTask());
            await protocol.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            var detachStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            detachTask = Task.Run(() =>
            {
                detachStarted.TrySetResult();
                context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
            });
            await detachStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.IsFalse(detachTask.IsCompleted);

            protocol.Release.TrySetResult();
            await writeTask.WaitAsync(TimeSpan.FromSeconds(1));
            await detachTask.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        }
        finally
        {
            protocol.Release.TrySetResult();
            if (writeTask is not null)
            {
                await writeTask.WaitAsync(TimeSpan.FromSeconds(1));
            }

            if (detachTask is not null)
            {
                await detachTask.WaitAsync(TimeSpan.FromSeconds(1));
            }

            await context.CleanupAsync();
        }
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

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        Assert.AreEqual("initial", context.ConnectionId);
        Assert.IsInstanceOfType<ConnectionContext>(context.TcpConnection);
        Assert.AreSame(context.Features, context.TcpConnection.Features);
        Assert.AreSame(context.Items, context.TcpConnection.Items);
        Assert.AreSame(stableFeature, context.Features.Get<object>());
        Assert.AreSame(stableFeature, context.Items["state"]);
        Assert.AreSame(protocol, context.Protocol);
        Assert.AreEqual(1002, context.LocalEndPoint!.Port);
        Assert.AreEqual(2002, context.RemoteEndPoint!.Port);

        await context.WriteAsync(new TestMessage());

        var result = await replacement.Output.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(42, result.Buffer.FirstSpan[0]);
        replacement.Output.Reader.AdvanceTo(result.Buffer.End);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StableTransportRelaysInputAndOutputAcrossPhysicalReplacement()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var stableTransport = context.TcpConnection.Transport;

        await initial.Input.Writer.WriteAsync(new byte[] { 1, 2, 3 });
        var initialRead = await stableTransport.Input.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, initialRead.Buffer.ToArray());
        stableTransport.Input.AdvanceTo(initialRead.Buffer.End);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        var boundary = await stableTransport.Input.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(boundary.IsCanceled);
        stableTransport.Input.AdvanceTo(boundary.Buffer.End);
        context.TcpConnection.AcknowledgeInputBoundary();

        await replacement.Input.Writer.WriteAsync(new byte[] { 4, 5, 6 });
        var replacementRead = await ReadNonCanceledAsync(stableTransport.Input);
        CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, replacementRead.Buffer.ToArray());
        stableTransport.Input.AdvanceTo(replacementRead.Buffer.End);

        await context.WriteAsync(new TestMessage());
        var outputRead = await replacement.Output.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(42, outputRead.Buffer.FirstSpan[0]);
        replacement.Output.Reader.AdvanceTo(outputRead.Buffer.End);

        Assert.AreSame(stableTransport, context.TcpConnection.Transport);
    }

    [TestMethod]
    public async Task TerminalConnectionLeavesStablePipeCompletionToItsOwners()
    {
        using var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.Abort();
        await context.CleanupAsync();
        context.TcpConnection.CompleteTransportInput();
        await AssertPipeReaderCompletedAsync(context.TcpConnection.Transport.Input);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task CleanupAsyncCancelsConnectionClosedAndCompletesAbortAsync()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = context.ConnectionAborted.Register(() => connectionClosed.TrySetResult());

        await context.CleanupAsync().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsTrue(connectionClosed.Task.IsCompletedSuccessfully);
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.AbortAsync().WaitAsync(TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LogicalCleanupCompletesAllStablePipeEnds()
    {
        var blockedWriter = TrackBlockingPipeWriter();
        blockedWriter.ReleaseOnCancel = false;
        using var initial = CreatePhysicalConnection("initial", outputWriter: blockedWriter);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.StablePipeCleanup");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            Substitute.For<IRaidoDispatcher>(),
            metrics);

        var run = handler.ConnectAsync(context);
        var write = context.WriteAsync(new TestMessage());
        await blockedWriter.FlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        context.Abort();
        Assert.IsFalse(run.IsCompleted);

        blockedWriter.Release();
        await write;
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await AssertPipeReaderCompletedAsync(context.TcpConnection.Transport.Input);
        await AssertPipeReaderCompletedAsync(context.TcpConnection.Application.Input);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LogicalCleanupAwaitsActiveHubWriter()
    {
        using var initial = CreatePhysicalConnection("initial");
        var protocol = new BlockingWriteProtocol();
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(protocol, CancellationToken.None);
        Task? write = null;
        Task? cleanup = null;
        var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            write = Task.Run(() => context.WriteAsync(new TestMessage()).AsTask());
            await protocol.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            cleanup = Task.Run(async () =>
            {
                cleanupStarted.TrySetResult();
                await context.CleanupAsync();
            });
            await cleanupStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsFalse(cleanup.IsCompleted);

            protocol.Release.TrySetResult();
            await write.WaitAsync(TimeSpan.FromSeconds(1));
            await cleanup.WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            protocol.Release.TrySetResult();
            if (write is not null)
            {
                await write.WaitAsync(TimeSpan.FromSeconds(1));
            }

            if (cleanup is not null)
            {
                await cleanup.WaitAsync(TimeSpan.FromSeconds(1));
            }
            else
            {
                await context.CleanupAsync();
            }
        }
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LogicalCleanupAwaitsActivePhysicalInputRelay()
    {
        using var blockingMemory = new BlockingMemoryManager(new byte[] { 7 });
        var inputBuffer = new ReadOnlySequence<byte>(blockingMemory.Memory);
        blockingMemory.Block = true;
        var inputReader = new SingleReadPipeReader(new ReadResult(
            inputBuffer,
            isCanceled: false,
            isCompleted: false));
        using var initial = CreatePhysicalConnection("initial", inputReader: inputReader);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        Task? cleanup = null;

        try
        {
            await blockingMemory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

            cleanup = context.CleanupAsync();
            Assert.IsFalse(cleanup.IsCompleted);

            blockingMemory.Release.TrySetResult();
            await cleanup.WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            blockingMemory.Release.TrySetResult();
            if (cleanup is not null)
            {
                await cleanup.WaitAsync(TimeSpan.FromSeconds(1));
            }
            else
            {
                await context.CleanupAsync();
            }
        }
    }

    [TestMethod]
    public async Task ReconnectWindowIsReusedForFailedCandidateAndRecreatedAfterLaterDisconnect()
    {
        var initial = CreatePhysicalConnection("initial");
        var failedCandidate = CreatePhysicalConnection("failed");
        var successfulCandidate = CreatePhysicalConnection("successful");
        var laterCandidate = CreatePhysicalConnection("later");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var firstPhysicalWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        failedCandidate.Closed.Cancel();
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(failedCandidate.Connection));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(successfulCandidate.Connection));
        Assert.IsTrue(await firstPhysicalWindow);
        context.TcpConnection.AcknowledgeInputBoundary();

        context.TcpConnection.OnPhysicalConnectionClosed(successfulCandidate.Connection);
        var secondPhysicalWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(ReferenceEquals(firstPhysicalWindow, secondPhysicalWindow));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(laterCandidate.Connection));
        Assert.IsTrue(await secondPhysicalWindow);
        context.TcpConnection.AcknowledgeInputBoundary();
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task TimedOutReconnectWindowIsTerminalAndCannotBeReopened()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromMilliseconds(1));

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsFalse(await context.TcpConnection.WaitForReconnectAsync(TimeSpan.Zero));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ReconnectDeadlineStartsAtPhysicalDetach()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var timeProvider = new ManualTimeProvider();
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(1), timeProvider: timeProvider);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        var physicalConnectionWindow = context.TcpConnection.WaitForReconnectAsync();

        Assert.IsTrue(physicalConnectionWindow.IsCompletedSuccessfully);
        Assert.IsFalse(await physicalConnectionWindow);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(replacement.Closed.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task StatefulReconnectTimeoutDisconnectsThroughHandlerExactlyOnce()
    {
        using var initial = CreatePhysicalConnection("initial");
        var timeProvider = new ManualTimeProvider();
        var context = CreateContext(
            initial.Connection,
            reconnectEnabled: true,
            timeout: TimeSpan.FromSeconds(1),
            timeProvider: timeProvider);
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var lifetimeConnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcherConnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lifetimeManager.OnConnectedAsync(context).Returns(_ =>
        {
            lifetimeConnected.TrySetResult();
            return Task.CompletedTask;
        });
        dispatcher.OnConnectedAsync(context).Returns(_ =>
        {
            dispatcherConnected.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.HandlerTimeout");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await lifetimeConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await dispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));

        initial.Input.Writer.Complete();
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        await run.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        await lifetimeManager.Received(1).OnConnectedAsync(context);
        await lifetimeManager.Received(1).OnDisconnectedAsync(context);
        await dispatcher.Received(1).OnConnectedAsync(context);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ExplicitAbortDuringPhysicalConnectionDisconnectsThroughHandlerExactlyOnce()
    {
        using var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var lifetimeConnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcherConnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lifetimeManager.OnConnectedAsync(context).Returns(_ =>
        {
            lifetimeConnected.TrySetResult();
            return Task.CompletedTask;
        });
        dispatcher.OnConnectedAsync(context).Returns(_ =>
        {
            dispatcherConnected.TrySetResult();
            return Task.CompletedTask;
        });
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.HandlerAbort");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await lifetimeConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await dispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));

        initial.Input.Writer.Complete();
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        context.Abort();

        await run.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        await lifetimeManager.Received(1).OnConnectedAsync(context);
        await lifetimeManager.Received(1).OnDisconnectedAsync(context);
        await dispatcher.Received(1).OnConnectedAsync(context);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
    }

    [TestMethod]
    public async Task StaleCloseCallbackCannotDetachReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        InvokeCloseRequested(context, initial.Connection);
        closeRequested.RequestClose();

        Assert.AreEqual(1000, context.LocalEndPoint!.Port);
        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task DetachedCloseRequestCallbackTerminalizesTheSameReconnectWindow()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var callbackReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumeCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var callback = Task.Run(async () =>
        {
            callbackReady.TrySetResult();
            await resumeCallback.Task;
            InvokeCloseRequested(context, initial.Connection);
        });

        await callbackReady.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);

        resumeCallback.TrySetResult();
        await callback.WaitAsync(TimeSpan.FromSeconds(1));
        await context.AbortAsync();

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task DetachedCloseRequestRegistrationRemainsTerminalForTheReconnectWindow()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        closeRequested.RequestClose();
        await context.AbortAsync();

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task CloseRequestBeforePhysicalActivationTerminatesInsteadOfPublishingCandidate()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var candidate = CreatePhysicalConnection("candidate");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var candidateHeartbeat = TrackBlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(candidateHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var activation = Task.Run(() => context.TcpConnection.TryAttachPhysicalConnection(candidate.Connection));
        await candidateHeartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        closeRequested.RequestClose();
        candidateHeartbeat.Release.TrySetResult();

        Assert.IsFalse(await activation.WaitAsync(TimeSpan.FromSeconds(1)));
        await context.AbortAsync();
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task StableHeartbeatHandlersContinueAcrossPhysicalReplacement()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        using var closeRequested = new CloseRequestedFeature();
        replacement.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        Assert.AreEqual(1, initialHeartbeat.Callbacks.Count);
        Assert.AreEqual(1, replacementHeartbeat.Callbacks.Count);
        initialHeartbeat.Run();
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);

        closeRequested.Source.Cancel();

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        await context.AbortAsync();
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(CreatePhysicalConnection("after-close-request").Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task KeepAliveGenerationFailureIsTerminalWhenPhysicalConnectionEnabled()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var failure = new IOException("ping generation failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        await context.SetProtocolAsync(new FailingOutputProtocol(messageBytesException: failure), CancellationToken.None);

        await InvokePingAsync(context);

        Assert.AreSame(failure, context.TcpConnection.TerminalException);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task KeepAlivePhysicalWriteFailureOpensAReconnectWindow()
    {
        var failingWriter = new DeferredFailingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: failingWriter);
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var ping = InvokePingAsync(context);
        failingWriter.Fail(new IOException("ping write failure"));
        await ping;
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));

        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StaleKeepAlivePhysicalWriteFailureCannotAbortReplacement()
    {
        var failingWriter = new DeferredFailingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: failingWriter);
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var ping = InvokePingAsync(context);
        await failingWriter.FlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        failingWriter.Fail(new IOException("stale ping write failure"));
        await ping;
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));

        Assert.IsFalse(context.ConnectionAborted.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task DetachedKeepAlivePingDoesNotBlockReplacementWritesOrAbort()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        await InvokePingAsync(context);
        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);

        await context.WriteAsync(new TestMessage());
        var read = await replacement.Output.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 42 }, read.Buffer.ToArray());
        replacement.Output.Reader.AdvanceTo(read.Buffer.End);

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out current));
        Assert.AreSame(replacement.Connection, current);

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAborted.IsCancellationRequested);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task DetachedKeepAlivePingDoesNotAdvanceStableSendTimestamp()
    {
        var clock = new ManualTimeProvider();
        var initialHeartbeat = new RecordingHeartbeatFeature();
        var replacementHeartbeat = new RecordingHeartbeatFeature();
        var replacementWriter = new RecordingPipeWriter();
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement", outputWriter: replacementWriter);
        initial.Connection.Features.Set<IConnectionHeartbeatFeature>(initialHeartbeat);
        replacement.Connection.Features.Set<IConnectionHeartbeatFeature>(replacementHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromMinutes(5), timeProvider: clock);
        context.OnConnectedAsync().GetAwaiter().GetResult();

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        clock.Advance(TimeSpan.FromMinutes(2));
        await InvokePingAsync(context);

        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        context.TcpConnection.AcknowledgeInputBoundary();
        replacementHeartbeat.Run();
        var ping = await replacementWriter.FirstFlush.Task.WaitAsync(TimeSpan.FromSeconds(1));

        CollectionAssert.AreEqual(new byte[] { 42 }, ping);
        await context.CleanupAsync();
    }

    [TestMethod]
    public async Task StoreMembershipSurvivesPhysicalReplacementUntilTerminalDisconnect()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var store = new RaidoHubConnectionStore();
        var lifetimeManager = new DefaultRaidoHubLifetimeManager(store);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.StorePhysicalConnection");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await WaitForConditionAsync(() => store[context.ConnectionId] is not null);
        Assert.AreSame(context, store[context.ConnectionId]);

        initial.Input.Writer.Complete();
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.AreSame(context, store[context.ConnectionId]);

        Assert.IsTrue(context.TcpConnection.TryAttachPhysicalConnection(replacement.Connection));
        await WaitForConditionAsync(() => context.TcpConnection.TryGetCurrentConnection(out var current) && ReferenceEquals(current, replacement.Connection));
        Assert.AreSame(context, store[context.ConnectionId]);

        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsNull(store[context.ConnectionId]);
        Assert.AreEqual(0, store.Count);
        await dispatcher.Received(1).OnConnectedAsync(context);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
        await context.CleanupAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ProtocolTransitionWaitsForBlockedOutboundWrite()
    {
        using var physical = CreatePhysicalConnection("initial");
        var protocolA = new BlockingWriteProtocol();
        var protocolB = new PayloadWritingProtocol { Payload = [43] };
        var context = CreateContext(physical.Connection, reconnectEnabled: false);
        await context.SetProtocolAsync(protocolA, CancellationToken.None);

        Task? write = null;
        Task? transition = null;
        try
        {
            write = Task.Run(() => context.WriteAsync(new TestMessage()).AsTask());
            await protocolA.WriteStarted.Task;

            transition = context.SetProtocolAsync(protocolB, CancellationToken.None).AsTask();
            Assert.IsFalse(transition.IsCompleted);

            protocolA.Release.TrySetResult();
            await write;
            await transition;

            var first = await physical.Output.Reader.ReadAsync();
            CollectionAssert.AreEqual(new byte[] { 42 }, first.Buffer.ToArray());
            physical.Output.Reader.AdvanceTo(first.Buffer.End);

            await context.WriteAsync(new TestMessage());
            var second = await physical.Output.Reader.ReadAsync();
            CollectionAssert.AreEqual(new byte[] { 43 }, second.Buffer.ToArray());
            physical.Output.Reader.AdvanceTo(second.Buffer.End);
        }
        finally
        {
            protocolA.Release.TrySetResult();
            if (write is not null)
            {
                await write;
            }

            if (transition is not null)
            {
                await transition;
            }

            await context.CleanupAsync();
        }
    }

    private TestHubConnectionContext CreateContext(
        ConnectionContext connection,
        bool reconnectEnabled,
        TimeSpan? timeout = null,
        TimeSpan? clientTimeout = null,
        TimeProvider? timeProvider = null)
    {
        var options = new RaidoConnectionContextOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1),
            ClientTimeoutInterval = clientTimeout ?? TimeSpan.FromMinutes(1),
            StatefulReconnectEnabled = reconnectEnabled,
            StatefulReconnectTimeout = timeout ?? TimeSpan.FromSeconds(5)
        };
        var tcpConnection = timeProvider is null
            ? new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance)
            : new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance, timeProvider);
        Assert.IsTrue(tcpConnection.TryAttachPhysicalConnection(connection));
        var context = new TestHubConnectionContext(
            tcpConnection,
            options,
            NullLoggerFactory.Instance,
            timeProvider ?? TimeProvider.System);

        _connections.Add(context);
        return context;
    }

    private sealed class TestHubConnectionContext(
        RaidoTcpConnectionContext tcpConnection,
        RaidoConnectionContextOptions options,
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider)
        : RaidoHubConnectionContext(
            tcpConnection,
            options,
            new PhysicalConnectionWritingProtocol(),
            loggerFactory,
            timeProvider)
    {
        public RaidoTcpConnectionContext TcpConnection { get; } = tcpConnection;
    }

    private sealed class NoopAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TrackingAsyncDisposable : IAsyncDisposable
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
    }

    private void AssertStatefulReconnectTimeoutRejected(TimeSpan timeout)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RaidoTcpConnectionContext(
            new RaidoConnectionContextOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(1),
                ClientTimeoutInterval = TimeSpan.FromMinutes(1),
                StatefulReconnectEnabled = true,
                StatefulReconnectTimeout = timeout
            },
            NullLoggerFactory.Instance));
    }

    private static ValueTask InvokePingAsync(RaidoHubConnectionContext context) =>
        (ValueTask)typeof(RaidoHubConnectionContext)
            .GetMethod("TryWritePingAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(context, Array.Empty<object>())!;

    private static void InvokeCloseRequested(TestHubConnectionContext context, ConnectionContext physicalConnection) =>
        typeof(RaidoTcpConnectionContext)
            .GetMethod("OnConnectionClosedRequested", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(context.TcpConnection, new object[] { physicalConnection });

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.IsTrue(condition(), "The expected condition was not reached within the test timeout.");
    }

    private static async Task<ReadResult> ReadNonCanceledAsync(PipeReader reader)
    {
        while (true)
        {
            var result = await reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            if (!result.IsCanceled || !result.Buffer.IsEmpty)
            {
                return result;
            }

            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        }
    }

    private static async Task AssertPipeReaderCompletedAsync(PipeReader reader)
    {
        ReadResult result;
        try
        {
            result = await reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (InvalidOperationException)
        {
            // A pipe may reject a read after its reader has already completed.
            return;
        }

        Assert.IsTrue(result.IsCompleted);
        reader.AdvanceTo(result.Buffer.End);
    }

    private PhysicalConnection CreatePhysicalConnection(
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

        var physical = new PhysicalConnection(connection, input, output);
        _physicalConnections.Add(physical);
        connection.ConnectionClosed.Returns(physical.Closed.Token);
        return physical;
    }

    private BlockingHeartbeatFeature TrackBlockingHeartbeatFeature()
    {
        var feature = new BlockingHeartbeatFeature();
        _blockingHeartbeatFeatures.Add(feature);
        return feature;
    }

    private BlockingPipeWriter TrackBlockingPipeWriter()
    {
        var writer = new BlockingPipeWriter();
        _blockingPipeWriters.Add(writer);
        return writer;
    }

    private sealed class DeferredFailingPipeWriter : PipeWriter
    {
        private readonly ArrayBufferWriter<byte> _buffer = new();
        private readonly TaskCompletionSource<FlushResult> _flush =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource FlushStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Fail(Exception exception) => _flush.TrySetException(exception);

        public override void Advance(int bytes) => _buffer.Advance(bytes);

        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

        public override void CancelPendingFlush() { }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            FlushStarted.TrySetResult();
            return new(_flush.Task.WaitAsync(cancellationToken));
        }

        public override void Complete(Exception? exception = null) { }
    }

    private sealed class BlockingPipeWriter : PipeWriter
    {
        private readonly ArrayBufferWriter<byte> _buffer = new();
        private readonly TaskCompletionSource<FlushResult> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource FlushStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ReleaseOnCancel { get; set; } = true;

        public void Release() => _release.TrySetResult(new FlushResult(false, false));

        public override void Advance(int bytes) => _buffer.Advance(bytes);

        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

        public override void CancelPendingFlush()
        {
            if (ReleaseOnCancel)
            {
                Release();
            }
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            FlushStarted.TrySetResult();
            return new(_release.Task);
        }

        public override void Complete(Exception? exception = null) { }
    }

    private sealed class RecordingPipeWriter : PipeWriter
    {
        private ArrayBufferWriter<byte> _buffer = new();

        public TaskCompletionSource<byte[]> FirstFlush { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void Advance(int bytes) => _buffer.Advance(bytes);

        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

        public override void CancelPendingFlush() { }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            FirstFlush.TrySetResult(_buffer.WrittenSpan.ToArray());
            _buffer = new ArrayBufferWriter<byte>();
            return new(new FlushResult(false, false));
        }

        public override void Complete(Exception? exception = null) { }
    }

    private sealed class ThrowingPipeReader : PipeReader
    {
        private readonly Exception _exception;

        public TaskCompletionSource ReadInvoked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ThrowingPipeReader(Exception exception) => _exception = exception;

        public override void AdvanceTo(SequencePosition consumed) { }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { }

        public override void CancelPendingRead() { }

        public override void Complete(Exception? exception = null) { }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ReadInvoked.TrySetResult();
            throw _exception;
        }

        public override bool TryRead(out ReadResult result)
        {
            result = default;
            throw _exception;
        }
    }

    private sealed class CancelBeforeReturningPipeReader(ReadResult result) : PipeReader
    {
        private readonly ReadResult _result = result;
        private int _returned;

        public TaskCompletionSource ReadReturned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Action? BeforeReturning { get; set; }

        public override void AdvanceTo(SequencePosition consumed) { }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { }

        public override void CancelPendingRead() { }

        public override void Complete(Exception? exception = null) { }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _returned, 1) != 0)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            BeforeReturning?.Invoke();
            ReadReturned.TrySetResult();
            return new ValueTask<ReadResult>(_result);
        }

        public override bool TryRead(out ReadResult result)
        {
            result = _result;
            return true;
        }
    }

    private sealed class SingleReadPipeReader(ReadResult result) : PipeReader
    {
        private readonly ReadResult _result = result;
        private int _read;

        public override void AdvanceTo(SequencePosition consumed) { }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { }

        public override void CancelPendingRead() { }

        public override void Complete(Exception? exception = null) { }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _read, 1) != 0)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return new ValueTask<ReadResult>(_result);
        }

        public override bool TryRead(out ReadResult result)
        {
            result = _result;
            return true;
        }
    }

    private sealed class PendingPipeReader : PipeReader, IDisposable
    {
        private readonly TaskCompletionSource<ReadResult> _read =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void AdvanceTo(SequencePosition consumed) { }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { }

        public override void CancelPendingRead() => _read.TrySetResult(new ReadResult(
            ReadOnlySequence<byte>.Empty,
            isCanceled: true,
            isCompleted: false));

        public override void Complete(Exception? exception = null) => _read.TrySetResult(new ReadResult(
            ReadOnlySequence<byte>.Empty,
            isCanceled: false,
            isCompleted: true));

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) =>
            new(_read.Task.WaitAsync(cancellationToken));

        public override bool TryRead(out ReadResult result)
        {
            result = default;
            return false;
        }

        public void Dispose() => Complete();
    }

    private sealed class BlockingMemoryManager(byte[] bytes) : MemoryManager<byte>
    {
        private readonly byte[] _bytes = bytes;

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Block { get; set; }

        public override Span<byte> GetSpan()
        {
            if (Block)
            {
                Entered.TrySetResult();
                Release.Task.GetAwaiter().GetResult();
            }

            return _bytes;
        }

        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

        public override void Unpin() { }

        protected override void Dispose(bool disposing) { }
    }

    private sealed class PhysicalConnection(ConnectionContext connection, Pipe input, Pipe output) : IDisposable
    {
        private int _disposed;

        public ConnectionContext Connection { get; } = connection;

        public Pipe Input { get; } = input;

        public Pipe Output { get; } = output;

        public CancellationTokenSource Closed { get; } = new();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Closed.Cancel();
            Input.Reader.Complete();
            Input.Writer.Complete();
            Output.Reader.Complete();
            Output.Writer.Complete();
            Closed.Dispose();
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref _timestamp);

        public void Advance(TimeSpan duration) => Interlocked.Add(ref _timestamp, duration.Ticks);
    }

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

    private sealed class InherentKeepAliveFeature(bool hasInherentKeepAlive) : IConnectionInherentKeepAliveFeature
    {
        public bool HasInherentKeepAlive { get; } = hasInherentKeepAlive;
    }

    private sealed class BlockingHeartbeatFeature : IConnectionHeartbeatFeature
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<(Action<object> Callback, object State)> Callbacks { get; } = new();

        public void OnHeartbeat(Action<object> action, object state)
        {
            Callbacks.Add((action, state));
            Entered.TrySetResult();
            Release.Task.GetAwaiter().GetResult();
        }

        public void Run()
        {
            foreach (var (callback, state) in Callbacks)
            {
                callback(state);
            }
        }
    }

    private sealed class CloseRequestedFeature : IConnectionLifetimeNotificationFeature, IDisposable
    {
        public CancellationTokenSource Source { get; } = new();

        public int RequestCloseCount { get; private set; }

        public CancellationToken ConnectionClosedRequested { get; set; }

        public CloseRequestedFeature() => ConnectionClosedRequested = Source.Token;

        public void RequestClose()
        {
            RequestCloseCount++;
            Source.Cancel();
        }

        public void Dispose() => Source.Dispose();
    }

    private sealed class UnknownPhysicalFeature { }

}
