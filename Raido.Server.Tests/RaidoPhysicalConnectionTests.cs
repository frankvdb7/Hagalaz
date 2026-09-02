using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
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
public sealed class RaidoPhysicalConnectionTests
{
    [TestMethod]
    public void BuilderOptInUsesTheConfiguredFinitePhysicalConnectionTimeout()
    {
        var options = new RaidoOptions { StatefulReconnectTimeout = TimeSpan.FromSeconds(7) };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<RaidoOptions>>(Options.Create(options));
        using var provider = services.BuildServiceProvider();
        var physical = CreatePhysicalConnection("initial");

        var context = new DefaultRaidoHubConnectionContextBuilder(provider)
            .Create()
            .WithConnection(physical.Connection)
            .WithProtocol(new PhysicalConnectionWritingProtocol())
            .WithStatefulReconnect()
            .Build();

        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);
        context.Cleanup();
    }

    [TestMethod]
    public void BuilderOptInUsesTheDefaultFinitePhysicalConnectionTimeout()
    {
        var options = new RaidoOptions();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<RaidoOptions>>(Options.Create(options));
        using var provider = services.BuildServiceProvider();
        var physical = CreatePhysicalConnection("initial");

        var context = new DefaultRaidoHubConnectionContextBuilder(provider)
            .Create()
            .WithConnection(physical.Connection)
            .WithProtocol(new PhysicalConnectionWritingProtocol())
            .WithStatefulReconnect()
            .Build();

        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);
        context.Cleanup();
    }

    [TestMethod]
    public void InfinitePhysicalConnectionTimeoutIsRejectedWhenActivationIsEnabled() =>
        AssertPhysicalConnectionTimeoutRejected(Timeout.InfiniteTimeSpan);

    [TestMethod]
    public void ZeroPhysicalConnectionTimeoutIsRejectedWhenActivationIsEnabled() =>
        AssertPhysicalConnectionTimeoutRejected(TimeSpan.Zero);

    [TestMethod]
    public void NegativePhysicalConnectionTimeoutIsRejectedWhenActivationIsEnabled() =>
        AssertPhysicalConnectionTimeoutRejected(TimeSpan.FromTicks(-1));

    [TestMethod]
    public void PhysicalConnectionTimeoutAboveTimerMaximumIsRejectedWhenActivationIsEnabled() =>
        AssertPhysicalConnectionTimeoutRejected(TimeSpan.FromMilliseconds(uint.MaxValue));

    [TestMethod]
    public void InvalidPhysicalConnectionTimeoutIsIgnoredWhenActivationIsDisabled()
    {
        using var physical = CreatePhysicalConnection("initial");

        var context = CreateContext(physical.Connection, reconnectEnabled: false, timeout: Timeout.InfiniteTimeSpan);

        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        context.Cleanup();
    }

    [TestMethod]
    public void PreSignalledConnectionClosedStartsDetachedPhysicalConnectionWindow()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        initial.Closed.Cancel();

        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        context.Cleanup();
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
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);

        closeRequested.RequestClose();

        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        Assert.IsTrue(context.TcpConnection.IsTerminal);

        await context.AbortAsync();

        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
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
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void PreSignalledPhysicalConnectionClosedAndCloseRequestedAreTerminal()
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
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task ProtocolWriteIOExceptionIsTerminalWhenActivationEnabled()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var failure = new IOException("encoder failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new FailingOutputProtocol(writeException: failure);

        await context.WriteAsync(new TestMessage());

        Assert.AreSame(failure, context.TcpConnection.TerminalException);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public async Task ProtocolWriteProgrammingFailureIsTerminalWhenActivationEnabled()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var failure = new InvalidOperationException("encoder failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new FailingOutputProtocol(writeException: failure);

        await context.WriteAsync(new TestMessage());

        Assert.AreSame(failure, context.TcpConnection.TerminalException);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public async Task CallerCancelledWriteDoesNotOpenPhysicalConnectionWindow()
    {
        using var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => context.WriteAsync(new TestMessage(), cancellation.Token).AsTask());

        Assert.IsNull(context.TcpConnection.TerminalException);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(initial.Connection, current);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task HandlerUsesAFreshReaderAfterReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
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
        using var meter = new Meter("Raido.Server.Tests.PhysicalConnection");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.RunAsync(context);
        initial.Closed.Cancel();
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(2));

        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task HandlerWaitsWhenItStartsWithDetachedPhysicalConnectionWindow()
    {
        var initial = CreatePhysicalConnection("initial");
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
        using var meter = new Meter("Raido.Server.Tests.DetachedHandler");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var run = handler.DispatchMessagesAsync(context);

        Assert.IsFalse(run.IsCompleted);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(2));

        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task TimeoutWinsWhilePhysicalCallbacksAreStillRegistering()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        var heartbeat = new BlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var activation = Task.Run(() => context.TcpConnection.TryActivatePersistentConnection(candidate.Connection));
        await heartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsFalse(await context.TcpConnection.WaitForReconnectAsync(TimeSpan.Zero));
        heartbeat.Release.TrySetResult();
        Assert.IsFalse(await activation.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(candidate.Closed.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(candidate.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
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
            Task.Run(() => context.TcpConnection.TryActivatePersistentConnection(first.Connection)),
            Task.Run(() => context.TcpConnection.TryActivatePersistentConnection(second.Connection)));

        Assert.AreEqual(1, results.Count(result => result));
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.IsTrue(ReferenceEquals(current, first.Connection) || ReferenceEquals(current, second.Connection));
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LatePhysicalCandidateFromTheFirstWindowCannotActivateInTheSecondWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var lateCandidate = CreatePhysicalConnection("late");
        var winner = CreatePhysicalConnection("winner");
        var next = CreatePhysicalConnection("next");
        var blockingHeartbeat = new BlockingHeartbeatFeature();
        lateCandidate.Connection.Features.Set<IConnectionHeartbeatFeature>(blockingHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        var lateActivation = Task.Run(() => context.TcpConnection.TryActivatePersistentConnection(lateCandidate.Connection));
        await blockingHeartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(winner.Connection));
        context.TcpConnection.OnPhysicalConnectionClosed(winner.Connection);

        blockingHeartbeat.Release.TrySetResult();
        Assert.IsFalse(await lateActivation.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(next.Connection));
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
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        var exception = new IOException("stale flush");
        pendingWriter.Fail(exception);
        await pendingWrite;

        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        context.Cleanup();
    }

    [TestMethod]
    public async Task DisabledConnectionTerminatesImmediatelyOnPhysicalLoss()
    {
        var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: false);

        initial.Closed.Cancel();

        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void DetachedConnectionHasNoEndpointsAndDoesNotExposePipes()
    {
        var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsNull(context.LocalEndPoint);
        Assert.IsNull(context.RemoteEndPoint);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = context.TcpConnection.Transport.Input);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = context.TcpConnection.Transport.Output);
        context.Cleanup();
    }

    [TestMethod]
    public async Task ExplicitAbortWhileDetachedCompletesThePhysicalConnectionWindowAndStaysTerminal()
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
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void AlreadyClosedPhysicalCandidateIsRejectedWithoutPublishingATransport()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        using var closeRequested = new CloseRequestedFeature();
        candidate.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        closeRequested.RequestClose();
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(candidate.Connection));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(closeRequested.ConnectionClosedRequested.IsCancellationRequested);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void RejectedPhysicalCandidateRemainsOwnedByTheCaller()
    {
        var initial = CreatePhysicalConnection("initial");
        var winner = CreatePhysicalConnection("winner");
        var rejected = CreatePhysicalConnection("rejected");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(winner.Connection));

        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(rejected.Connection));
        Assert.IsFalse(rejected.Closed.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
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
        var exception = new IOException("current flush");
        pendingWriter.Fail(exception);
        await pendingWrite;

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsNull(context.TcpConnection.TerminalException);

        var physicalConnectionWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        Assert.IsTrue(await physicalConnectionWindow);
        Assert.IsNull(context.TcpConnection.TerminalException);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public void StaleTransportFailureCannotDetachTheReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        var exception = new IOException("stale read");
        Assert.IsTrue(context.TcpConnection.HandleTransportFailure(initial.Connection, exception));

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsNull(context.TcpConnection.TerminalException);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
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
        context.Protocol = new TestProtocol { MessageToReturn = message };
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
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.DispatchMessagesAsync(context);

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        await replacement.Input.Writer.WriteAsync(new byte[] { 1 });
        replacement.Input.Writer.Complete();
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        context.Abort();

        await run.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsNull(context.TcpConnection.TerminalException);
        await dispatcher.Received(1).DispatchMessageAsync(context, message);
        context.Cleanup();
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
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            Substitute.For<IRaidoDispatcher>(),
            metrics);

        var run = handler.DispatchMessagesAsync(context);
        await failingReader.ReadInvoked.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Abort();
        await context.AbortAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalCandidateClosingDuringRegistrationIsRejectedAndLeavesTheWindowOpen()
    {
        var initial = CreatePhysicalConnection("initial");
        var candidate = CreatePhysicalConnection("candidate");
        var winner = CreatePhysicalConnection("winner");
        var heartbeat = new BlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(heartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var candidateActivation = Task.Run(() => context.TcpConnection.TryActivatePersistentConnection(candidate.Connection));
        await heartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        candidate.Closed.Cancel();
        heartbeat.Release.TrySetResult();

        Assert.IsFalse(await candidateActivation.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));

        var physicalConnectionWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(winner.Connection));
        Assert.IsTrue(await physicalConnectionWindow);
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
        var context = CreateContext(initial.Connection, reconnectEnabled: true, clientTimeout: TimeSpan.Zero);

        context.OnConnectedAsync().GetAwaiter().GetResult();
        context.StartClientTimeout();
        context.BeginClientTimeout();
        context.StopClientTimeout();
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        Assert.AreEqual(2, initialHeartbeat.Callbacks.Count);
        Assert.AreEqual(2, replacementHeartbeat.Callbacks.Count);

        replacementHeartbeat.Run();
        initialHeartbeat.Run();

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public async Task ProtocolParserFailureIsTerminalAndCannotOpenPhysicalConnectionWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var parserFailure = new InvalidOperationException("parser failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new TestProtocol { ParseException = parserFailure };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ParserFailure");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.RunAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, parserFailure);
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task ParserIOExceptionIsTerminalWhenItDoesNotComeFromThePhysicalConnectionRead()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var parserFailure = new IOException("parser failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new TestProtocol { ParseException = parserFailure };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ParserIOException");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.RunAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, parserFailure);
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task ParserCancellationIsTerminalWhenItDoesNotComeFromThePhysicalConnectionRead()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var parserFailure = new OperationCanceledException("parser failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new TestProtocol { ParseException = parserFailure };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ParserCancellation");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.RunAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, parserFailure);
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task IncompleteProtocolDataIsTerminalAndCannotOpenPhysicalConnectionWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new TestProtocol { ParseMessageReturns = false };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.IncompleteProtocolData");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.RunAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        initial.Input.Writer.Complete();
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Is<Exception?>(exception => exception is InvalidDataException));
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task OversizedProtocolDataIsTerminalAndCannotOpenPhysicalConnectionWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new TestProtocol { ParseMessageReturns = false };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.OversizedProtocolData");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions { MaximumReceiveMessageSize = 0 }),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.RunAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Is<Exception?>(exception => exception is InvalidDataException));
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task ApplicationDispatchFailureIsTerminalAndCannotOpenPhysicalConnectionWindow()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var message = new TestMessage();
        var dispatchFailure = new InvalidOperationException("dispatch failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new TestProtocol { MessageToReturn = message };
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        dispatcher.DispatchMessageAsync(context, message).Returns(Task.FromException(dispatchFailure));
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.ApplicationFailure");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoLifetimeManager>(),
            dispatcher,
            metrics);

        var run = handler.RunAsync(context);
        await initial.Input.Writer.WriteAsync(new byte[] { 1 });
        await run.WaitAsync(TimeSpan.FromSeconds(1));

        await dispatcher.Received(1).OnDisconnectedAsync(context, dispatchFailure);
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
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
        using var meter = new Meter("Raido.Server.Tests.PhysicalConnectionLifetime");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        initial.Closed.Cancel();
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
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

    [TestMethod]
    public async Task TcpReplacementDoesNotChangeHubProtocol()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        var originalProtocol = context.Protocol;

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var physicalConnectionWaiter = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        Assert.AreSame(originalProtocol, context.Protocol);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        Assert.IsTrue(await physicalConnectionWaiter);

        context.Cleanup();
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
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

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

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
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

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
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
        context.Cleanup();
    }

    [TestMethod]
    public async Task PhysicalConnectionWindowIsReusedForFailedCandidateAndRecreatedAfterLaterDisconnect()
    {
        var initial = CreatePhysicalConnection("initial");
        var failedCandidate = CreatePhysicalConnection("failed");
        var successfulCandidate = CreatePhysicalConnection("successful");
        var laterCandidate = CreatePhysicalConnection("later");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var firstPhysicalWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        failedCandidate.Closed.Cancel();
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(failedCandidate.Connection));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(successfulCandidate.Connection));
        Assert.IsTrue(await firstPhysicalWindow);

        context.TcpConnection.OnPhysicalConnectionClosed(successfulCandidate.Connection);
        var secondPhysicalWindow = context.TcpConnection.WaitForReconnectAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(ReferenceEquals(firstPhysicalWindow, secondPhysicalWindow));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(laterCandidate.Connection));
        Assert.IsTrue(await secondPhysicalWindow);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task TimedOutPhysicalConnectionWindowIsTerminalAndCannotBeReopened()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromMilliseconds(1));

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);

        Assert.IsFalse(await context.TcpConnection.WaitForReconnectAsync(TimeSpan.Zero));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalConnectionDeadlineStartsAtDetach()
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
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(replacement.Closed.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalConnectionTimeoutDisconnectsThroughHandlerExactlyOnce()
    {
        using var initial = CreatePhysicalConnection("initial");
        var timeProvider = new ManualTimeProvider();
        var context = CreateContext(
            initial.Connection,
            reconnectEnabled: true,
            timeout: TimeSpan.FromSeconds(1),
            timeProvider: timeProvider);
        var lifetimeManager = Substitute.For<IRaidoLifetimeManager>();
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
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await lifetimeConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await dispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));

        initial.Closed.Cancel();
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
        var lifetimeManager = Substitute.For<IRaidoLifetimeManager>();
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
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await lifetimeConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await dispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(1));

        initial.Closed.Cancel();
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
    public void StaleCloseCallbackCannotDetachReplacement()
    {
        var initial = CreatePhysicalConnection("initial");
        var replacement = CreatePhysicalConnection("replacement");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        InvokeCloseRequested(context, initial.Connection);
        closeRequested.RequestClose();

        Assert.AreEqual(1000, context.LocalEndPoint!.Port);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task DetachedCloseRequestCallbackTerminalizesTheSamePhysicalConnectionWindow()
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
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task DetachedCloseRequestRegistrationRemainsTerminalForThePhysicalConnectionWindow()
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
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task CloseRequestBeforePhysicalActivationTerminatesInsteadOfPublishingCandidate()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var candidate = CreatePhysicalConnection("candidate");
        using var closeRequested = new CloseRequestedFeature();
        initial.Connection.Features.Set<IConnectionLifetimeNotificationFeature>(closeRequested);
        var candidateHeartbeat = new BlockingHeartbeatFeature();
        candidate.Connection.Features.Set<IConnectionHeartbeatFeature>(candidateHeartbeat);
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        var activation = Task.Run(() => context.TcpConnection.TryActivatePersistentConnection(candidate.Connection));
        await candidateHeartbeat.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        closeRequested.RequestClose();
        candidateHeartbeat.Release.TrySetResult();

        Assert.IsFalse(await activation.WaitAsync(TimeSpan.FromSeconds(1)));
        await context.AbortAsync();
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PhysicalCallbacksAreRegisteredOnTheReplacementAndStaleHeartbeatIsIgnored()
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
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        Assert.AreEqual(1, initialHeartbeat.Callbacks.Count);
        Assert.AreEqual(1, replacementHeartbeat.Callbacks.Count);
        initialHeartbeat.Run();
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);

        closeRequested.Source.Cancel();

        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        await context.AbortAsync();
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(CreatePhysicalConnection("after-close-request").Connection));
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task KeepAliveGenerationFailureIsTerminalWhenPhysicalConnectionEnabled()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var failure = new IOException("ping generation failure");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);
        context.Protocol = new FailingOutputProtocol(messageBytesException: failure);

        await InvokePingAsync(context, initial.Connection);

        Assert.AreSame(failure, context.TcpConnection.TerminalException);
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsFalse(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public async Task KeepAlivePhysicalWriteFailureOpensAPhysicalConnectionWindow()
    {
        var failingWriter = new DeferredFailingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: failingWriter);
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var ping = InvokePingAsync(context, initial.Connection);
        failingWriter.Fail(new IOException("ping write failure"));
        await ping;

        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        context.Cleanup();
    }

    [TestMethod]
    public async Task StaleKeepAlivePhysicalWriteFailureCannotAbortReplacement()
    {
        var failingWriter = new DeferredFailingPipeWriter();
        using var initial = CreatePhysicalConnection("initial", outputWriter: failingWriter);
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        var ping = InvokePingAsync(context, initial.Connection);
        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));

        failingWriter.Fail(new IOException("stale ping write failure"));
        await ping;

        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task StaleKeepAlivePingDoesNotBlockReplacementWritesOrAbort()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true);

        context.TcpConnection.OnPhysicalConnectionClosed(initial.Connection);
        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out var current));
        Assert.AreSame(replacement.Connection, current);

        await InvokePingAsync(context, initial.Connection);

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out current));
        Assert.AreSame(replacement.Connection, current);

        await context.WriteAsync(new TestMessage());
        var read = await replacement.Output.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        CollectionAssert.AreEqual(new byte[] { 42 }, read.Buffer.ToArray());
        replacement.Output.Reader.AdvanceTo(read.Buffer.End);

        Assert.IsTrue(context.TcpConnection.TryGetCurrentConnection(out current));
        Assert.AreSame(replacement.Connection, current);

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task ClientTimeoutTerminalizationDoesNotDeadlockWithConnectionClosedCallback()
    {
        using var initial = CreatePhysicalConnection("initial");
        var context = CreateContext(initial.Connection, reconnectEnabled: false, clientTimeout: TimeSpan.Zero);
        context.StartClientTimeout();
        context.BeginClientTimeout();

        var timeoutLock = (Lock)typeof(RaidoHubConnectionContext)
            .GetField("_receiveMessageTimeoutLock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(context)!;
        var cancellationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var marker = initial.Closed.Token.Register(cancellationStarted.SetResult);
        Task close;
        Task check;

        lock (timeoutLock)
        {
            close = Task.Run(() => initial.Closed.Cancel());
            Assert.IsTrue(cancellationStarted.Task.Wait(TimeSpan.FromSeconds(1)));
            check = Task.Run(() => InvokeClientTimeoutCheck(context, initial.Connection));
        }

        await Task.WhenAll(close, check).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(context.TcpConnection.IsTerminal);
        Assert.IsFalse(context.TcpConnection.IsReconnectEnabled);
        Assert.IsFalse(context.TcpConnection.TryGetCurrentConnection(out _));

        await context.AbortAsync();
        Assert.IsTrue(context.ConnectionAbortedToken.IsCancellationRequested);
        context.Cleanup();
    }

    [TestMethod]
    public async Task StoreMembershipSurvivesPhysicalReplacementUntilTerminalDisconnect()
    {
        using var initial = CreatePhysicalConnection("initial");
        using var replacement = CreatePhysicalConnection("replacement");
        var context = CreateContext(initial.Connection, reconnectEnabled: true, timeout: TimeSpan.FromSeconds(5));
        var store = new RaidoConnectionStore();
        var lifetimeManager = new DefaultRaidoLifetimeManager(store);
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        var meterFactory = Substitute.For<IMeterFactory>();
        using var meter = new Meter("Raido.Server.Tests.StorePhysicalConnection");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        using var metrics = new RaidoMetrics(meterFactory);
        var handler = new RaidoConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            metrics);

        var run = handler.ConnectAsync(context);
        await WaitForConditionAsync(() => store[context.ConnectionId] is not null);
        Assert.AreSame(context, store[context.ConnectionId]);

        initial.Closed.Cancel();
        await WaitForConditionAsync(() => !context.TcpConnection.TryGetCurrentConnection(out _));
        Assert.AreSame(context, store[context.ConnectionId]);

        Assert.IsTrue(context.TcpConnection.TryActivatePersistentConnection(replacement.Connection));
        await WaitForConditionAsync(() => context.TcpConnection.TryGetCurrentConnection(out var current) && ReferenceEquals(current, replacement.Connection));
        Assert.AreSame(context, store[context.ConnectionId]);

        context.Abort();
        await run.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsNull(store[context.ConnectionId]);
        Assert.AreEqual(0, store.Count);
        await dispatcher.Received(1).OnConnectedAsync(context);
        await dispatcher.Received(1).OnDisconnectedAsync(context, Arg.Any<Exception?>());
        context.Cleanup();
    }

    private static RaidoHubConnectionContext CreateContext(
        ConnectionContext connection,
        bool reconnectEnabled,
        TimeSpan? timeout = null,
        TimeSpan? clientTimeout = null,
        TimeProvider? timeProvider = null)
    {
        var options = new RaidoHubConnectionContextOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1),
            ClientTimeoutInterval = clientTimeout ?? TimeSpan.FromMinutes(1),
            StatefulReconnectEnabled = reconnectEnabled,
            StatefulReconnectTimeout = timeout ?? TimeSpan.FromSeconds(5)
        };
        var context = timeProvider is null
            ? new RaidoHubConnectionContext(connection, options, NullLoggerFactory.Instance)
            : new RaidoHubConnectionContext(
                new RaidoTcpConnectionContext(connection, options, NullLoggerFactory.Instance, timeProvider),
                options,
                NullLoggerFactory.Instance,
                timeProvider);

        context.Protocol = new PhysicalConnectionWritingProtocol();
        return context;
    }

    private static void AssertPhysicalConnectionTimeoutRejected(TimeSpan timeout)
    {
        using var physical = CreatePhysicalConnection("initial");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RaidoHubConnectionContext(
            physical.Connection,
            new RaidoHubConnectionContextOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(1),
                ClientTimeoutInterval = TimeSpan.FromMinutes(1),
                StatefulReconnectEnabled = true,
                StatefulReconnectTimeout = timeout
            },
            NullLoggerFactory.Instance));
    }

    private static ValueTask InvokePingAsync(RaidoHubConnectionContext context, ConnectionContext physicalConnection) =>
        (ValueTask)typeof(RaidoHubConnectionContext)
            .GetMethod("TryWritePingAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(context, new object[] { physicalConnection })!;

    private static void InvokeCloseRequested(RaidoHubConnectionContext context, ConnectionContext physicalConnection) =>
        typeof(RaidoTcpConnectionContext)
            .GetMethod("OnConnectionClosedRequested", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(context.TcpConnection, new object[] { physicalConnection });

    private static void InvokeClientTimeoutCheck(RaidoHubConnectionContext context, ConnectionContext physicalConnection) =>
        typeof(RaidoHubConnectionContext)
            .GetMethod("CheckClientTimeoutForConnection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(context, new object[] { physicalConnection });

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

        var physical = new PhysicalConnection(connection, input, output);
        connection.ConnectionClosed.Returns(physical.Closed.Token);
        return physical;
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

    private sealed class PhysicalConnection(ConnectionContext connection, Pipe input, Pipe output) : IDisposable
    {
        public ConnectionContext Connection { get; } = connection;

        public Pipe Input { get; } = input;

        public Pipe Output { get; } = output;

        public CancellationTokenSource Closed { get; } = new();

        public void Dispose() => Closed.Dispose();
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

    private sealed class CloseRequestedFeature : IConnectionLifetimeNotificationFeature, IDisposable
    {
        public CancellationTokenSource Source { get; } = new();

        public CancellationToken ConnectionClosedRequested { get; set; }

        public CloseRequestedFeature() => ConnectionClosedRequested = Source.Token;

        public void RequestClose()
        {
            Source.Cancel();
        }

        public void Dispose() => Source.Dispose();
    }
}
