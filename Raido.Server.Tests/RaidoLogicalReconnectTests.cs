using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Messages;
using Raido.Common.Protocol;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoLogicalReconnectTests
{
    private sealed class TestMessage : RaidoMessage
    {
    }

    private sealed class WritingProtocol : IRaidoProtocol
    {
        private readonly byte _value;
        private readonly int _length;

        public WritingProtocol(byte value = 42, int length = 1)
        {
            _value = value;
            _length = length;
        }

        public string Name => "reconnect";
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
            var value = message is PingMessage ? (byte)9 : _value;
            output.GetSpan(_length).Slice(0, _length).Fill(value);
            output.Advance(_length);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => new[] { message is PingMessage ? (byte)9 : _value };

        public bool IsVersionSupported(int version) => version == 1;
    }

    private sealed class PhysicalConnection : IDisposable
    {
        public readonly CancellationTokenSource Closed = new();
        public readonly Pipe Input = new();
        public readonly Pipe Output;
        public readonly ConnectionContext Context;

        public PhysicalConnection(string id, IFeatureCollection? features = null, PipeWriter? outputWriter = null, PipeOptions? outputOptions = null)
        {
            Output = new Pipe(outputOptions ?? new PipeOptions());
            var transport = Substitute.For<IDuplexPipe>();
            transport.Input.Returns(Input.Reader);
            transport.Output.Returns(outputWriter ?? Output.Writer);
            var context = Substitute.For<ConnectionContext>();
            context.ConnectionId.Returns(id);
            context.Transport.Returns(transport);
            context.Features.Returns(features ?? new FeatureCollection());
            context.Items.Returns(new Dictionary<object, object?>());
            context.ConnectionClosed.Returns(Closed.Token);
            Context = context;
        }

        public void Dispose()
        {
            Closed.Dispose();
            Input.Reader.Complete();
            Output.Reader.Complete();
        }
    }

    private sealed class FailingPipeWriter : PipeWriter
    {
        public override void Advance(int bytesWritten) { }

        public override Memory<byte> GetMemory(int sizeHint = 0) => new byte[Math.Max(sizeHint, 1)];

        public override Span<byte> GetSpan(int sizeHint = 0) => new byte[Math.Max(sizeHint, 1)];

        public override void CancelPendingFlush() { }

        public override void Complete(Exception? exception = null) { }

        public override ValueTask CompleteAsync(Exception? exception = null) => ValueTask.CompletedTask;

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromException<FlushResult>(new IOException("The physical transport failed."));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, dueTime != Timeout.InfiniteTimeSpan);
            _timers.Add(timer);
            return timer;
        }

        public void FireTimers()
        {
            foreach (var timer in _timers.ToArray())
            {
                timer.Fire();
            }
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private bool _enabled;
            private bool _disposed;

            public ManualTimer(TimerCallback callback, object? state, bool enabled)
            {
                _callback = callback;
                _state = state;
                _enabled = enabled;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                _enabled = dueTime != Timeout.InfiniteTimeSpan;
                return !_disposed;
            }

            public void Fire()
            {
                if (_enabled && !_disposed)
                {
                    _callback(_state);
                }
            }

            public void Dispose() => _disposed = true;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    [TestMethod]
    public async Task NonOptedInTransportLossClosesImmediately()
    {
        using var physical = new PhysicalConnection("physical-1");
        var context = CreateContext(physical, statefulReconnect: true, enableReconnect: false);
        var store = new RaidoConnectionStore();
        store.Add(context);

        physical.Closed.Cancel();
        await context.AbortAsync();

        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, context.LifecycleState);
        store.Remove(context);
        Assert.IsNull(store[context.ConnectionId]);
    }

    [TestMethod]
    public async Task OptedInTransportLossRetainsLogicalConnectionUntilGraceExpiry()
    {
        using var physical = new PhysicalConnection("physical-1");
        var timeProvider = new ManualTimeProvider();
        var context = CreateContext(physical, statefulReconnect: true, timeProvider: timeProvider);
        var store = new RaidoConnectionStore();
        store.Add(context);

        physical.Closed.Cancel();

        Assert.AreEqual(RaidoConnectionLifecycleState.Reconnecting, context.LifecycleState);
        Assert.AreSame(context, store[context.ConnectionId]);
        Assert.IsFalse(context.ConnectionAbortedToken.IsCancellationRequested);

        timeProvider.FireTimers();
        await context.AbortAsync();

        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, context.LifecycleState);
        store.Remove(context);
        Assert.IsNull(store[context.ConnectionId]);
    }

    [TestMethod]
    public async Task RebindKeepsLogicalIdentityAndWritesOnlyToWinningPhysicalTransport()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var features = new FeatureCollection();
        original.Context.Features.Returns(features);
        var context = CreateContext(original, statefulReconnect: true);
        context.Protocol = new WritingProtocol();
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        var logicalId = context.ConnectionId;

        original.Closed.Cancel();
        var rebound = await store.TryPrepareRebindAsync(logicalId, replacement);

        Assert.IsNotNull(rebound);
        await CommitTransferAsync(replacement);
        Assert.AreEqual(RaidoConnectionLifecycleState.Connected, context.LifecycleState);
        Assert.AreEqual(logicalId, context.ConnectionId);
        Assert.AreEqual("physical-2", context.PhysicalConnectionId);
        Assert.AreSame(features, context.Features);
        Assert.AreSame(context, store[logicalId]);

        await context.WriteAsync(new TestMessage());
        var result = await replacementPhysical.Output.Reader.ReadAsync();
        Assert.AreEqual(42, result.Buffer.FirstSpan[0]);
        replacementPhysical.Output.Reader.AdvanceTo(result.Buffer.End);

        if (original.Output.Reader.TryRead(out var oldResult) && oldResult.Buffer.Length > 0)
        {
            original.Output.Reader.AdvanceTo(oldResult.Buffer.End);
            Assert.Fail("The detached physical transport received a message.");
        }
        context.Abort();
    }

    [TestMethod]
    public async Task RebindFlushesCommittedResponseAndPostCommitWorkBeforeNormalTraffic()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        replacement.Protocol = new WritingProtocol(1);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        var reservation = await store.TryPrepareRebindAsync(context.ConnectionId, replacement, new WritingProtocol(2));
        Assert.IsNotNull(reservation);
        Task? normalWrite = null;
        var normalWriteAttempted = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        reservation!.SetReconnectActions(
            successProxy =>
            {
                Assert.AreEqual(RaidoConnectionLifecycleState.Reconnecting, context.LifecycleState);
                return successProxy.SendAsync(new TestMessage());
            },
            async resyncProxy =>
        {
            Assert.AreEqual(RaidoConnectionLifecycleState.Reconnecting, context.LifecycleState);
            using (ExecutionContext.SuppressFlow())
            {
                normalWrite = Task.Run(async () =>
                {
                    var pending = context.WriteAsync(new TestMessage(), new WritingProtocol(3)).AsTask();
                    normalWriteAttempted.TrySetResult(pending);
                    await pending;
                });
            }

            var pendingNormalWrite = await normalWriteAttempted.Task;
            Assert.IsFalse(pendingNormalWrite.IsCompleted);
            var ping = (ValueTask)typeof(RaidoConnectionContext)
                .GetMethod("TryWritePingAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(context, null)!;
            await ping;
            await resyncProxy.SendAsync(new TestMessage());
        });
        context.Features.Get<IRaidoStatefulReconnectFeature>()!.OnReconnected(_ =>
            Task.CompletedTask);

        await CommitTransferAsync(replacement);
        await normalWrite!;

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, await ReadOutputAsync(replacementPhysical.Output.Reader, 3));
        context.Abort();
    }

    [TestMethod]
    public async Task ReconnectProxyRejectsSendsAfterTheHandoff()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        IRaidoClientProxy? reconnectProxy = null;
        var reservation = await store.TryPrepareRebindAsync(context.ConnectionId, replacement);
        Assert.IsNotNull(reservation);
        reservation!.SetReconnectActions(
            successProxy =>
            {
                reconnectProxy = successProxy;
                return Task.CompletedTask;
            },
            _ => Task.CompletedTask);

        await CommitTransferAsync(replacement);

        Assert.IsNotNull(reconnectProxy);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => reconnectProxy!.SendAsync(new TestMessage()));
        context.Abort();
    }

    [TestMethod]
    public async Task RebindDiscardsUncertainRetainedOutputBeforeReconnectResponse()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        replacement.Protocol = new WritingProtocol(1);
        var store = new RaidoConnectionStore();
        store.Add(context);

        original.Closed.Cancel();
        await context.Output.WriteAsync(new byte[] { 99 });

        var reservation = await store.TryPrepareRebindAsync(context.ConnectionId, replacement);
        Assert.IsNotNull(reservation);
        reservation!.SetReconnectActions(
            successProxy => successProxy.SendAsync(new TestMessage()),
            _ => Task.CompletedTask);

        await CommitTransferAsync(replacement);

        CollectionAssert.AreEqual(new byte[] { 1 }, await ReadOutputAsync(replacementPhysical.Output.Reader, 1));
        context.Abort();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RebindCancelsBlockedPreLossFlushBeforeTakingTheTargetWriteLock()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(
            original,
            statefulReconnect: true,
            applicationOutputOptions: new PipeOptions(pauseWriterThreshold: 2, resumeWriterThreshold: 1),
            startPhysicalSession: false);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        replacement.Protocol = new WritingProtocol(1);
        var store = new RaidoConnectionStore();
        store.Add(context);

        var staleWrite = context.WriteAsync(new TestMessage(), new WritingProtocol(99, length: 2)).AsTask();
        Assert.IsFalse(staleWrite.IsCompleted);

        original.Closed.Cancel();
        Assert.AreEqual(RaidoConnectionLifecycleState.Reconnecting, context.LifecycleState);

        var reservation = await store.TryPrepareRebindAsync(context.ConnectionId, replacement);
        Assert.IsNotNull(reservation);
        reservation!.SetReconnectActions(
            successProxy => successProxy.SendAsync(new TestMessage()),
            _ => Task.CompletedTask);

        await CommitTransferAsync(replacement);
        await staleWrite;

        Assert.AreEqual(RaidoConnectionLifecycleState.Connected, context.LifecycleState);
        Assert.IsNull(context.CloseException);
        await context.WriteAsync(new TestMessage(), new WritingProtocol(2));

        CollectionAssert.AreEqual(new byte[] { 1, 2 }, await ReadOutputAsync(replacementPhysical.Output.Reader, 2));
        context.Abort();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RebindConsumesLargePostCommitOutputWithoutBackpressureDeadlock()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection(
            "physical-2",
            outputOptions: new PipeOptions(pauseWriterThreshold: 1_000_000, resumeWriterThreshold: 500_000));
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        replacement.Protocol = new WritingProtocol(1);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        const int resyncMessageCount = 70_000;
        var reservation = await store.TryPrepareRebindAsync(context.ConnectionId, replacement, new WritingProtocol(2));
        Assert.IsNotNull(reservation);
        reservation!.SetReconnectActions(
            successProxy => successProxy.SendAsync(new TestMessage()),
            async resyncProxy =>
        {
            for (var i = 0; i < resyncMessageCount; i++)
            {
                await resyncProxy.SendAsync(new TestMessage());
            }
        });

        await CommitTransferAsync(replacement);

        var output = await ReadOutputAsync(replacementPhysical.Output.Reader, resyncMessageCount + 1);
        Assert.AreEqual(resyncMessageCount + 1, output.Length);
        Assert.AreEqual(1, output[0]);
        CollectionAssert.AreEqual(new byte[] { 2 }, output.Skip(1).Distinct().ToArray());
        context.Abort();
    }

    [TestMethod]
    public async Task InvalidatedReservationAbortsTheTemporaryReconnectConnection()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        var reservation = await store.TryPrepareRebindAsync(context.ConnectionId, replacement);
        Assert.IsNotNull(reservation);

        reservation!.Invalidate();
        await replacement.AbortAsync();

        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, replacement.LifecycleState);
        Assert.IsTrue(replacement.ConnectionAbortedToken.IsCancellationRequested);
        Assert.AreEqual(RaidoConnectionLifecycleState.Reconnecting, context.LifecycleState);
        context.Abort();
    }

    [TestMethod]
    public async Task GraceExpiryAfterPrepareAbortsTheTemporaryReconnectConnection()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var timeProvider = new ManualTimeProvider();
        var context = CreateContext(original, statefulReconnect: true, timeProvider: timeProvider);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        Assert.IsNotNull(await store.TryPrepareRebindAsync(context.ConnectionId, replacement));
        timeProvider.FireTimers();

        await replacement.AbortAsync();
        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, replacement.LifecycleState);
        Assert.IsTrue(replacement.ConnectionAbortedToken.IsCancellationRequested);
        context.Abort();
    }

    [TestMethod]
    public async Task LogicalAbortAfterPrepareAbortsTheTemporaryReconnectConnection()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        Assert.IsNotNull(await store.TryPrepareRebindAsync(context.ConnectionId, replacement));
        context.Abort();

        await replacement.AbortAsync();
        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, replacement.LifecycleState);
        Assert.IsTrue(replacement.ConnectionAbortedToken.IsCancellationRequested);
    }

    [TestMethod]
    public async Task RebindCanInstallReplacementProtocolBeforeTheFirstWrite()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var replacementProtocol = new WritingProtocol(43);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        var rebound = await store.TryPrepareRebindAsync(context.ConnectionId, replacement, replacementProtocol);

        Assert.IsNotNull(rebound);
        await CommitTransferAsync(replacement);
        Assert.AreSame(replacementProtocol, context.Protocol);

        await context.WriteAsync(new TestMessage());
        var result = await replacementPhysical.Output.Reader.ReadAsync();
        Assert.AreEqual(43, result.Buffer.FirstSpan[0]);
        replacementPhysical.Output.Reader.AdvanceTo(result.Buffer.End);
        context.Abort();
    }

    [TestMethod]
    public async Task RebindInvokesTheLogicalReconnectCallbackAfterTransportAttachment()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        var callbackOutput = default(PipeWriter);
        context.Features.Get<IRaidoStatefulReconnectFeature>()!.OnReconnected(output =>
        {
            callbackOutput = output;
            return Task.CompletedTask;
        });

        Assert.IsNotNull(await store.TryPrepareRebindAsync(context.ConnectionId, replacement));
        await CommitTransferAsync(replacement);
        Assert.AreSame(context.Output, callbackOutput);
        context.Abort();
    }

    [TestMethod]
    public async Task ReconnectedCallbackRemainsRegisteredAcrossRepeatedRebinds()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementOne = new PhysicalConnection("physical-2");
        using var replacementTwo = new PhysicalConnection("physical-3");
        var context = CreateContext(original, statefulReconnect: true);
        var replacementContextOne = CreateContext(replacementOne, statefulReconnect: true);
        var replacementContextTwo = CreateContext(replacementTwo, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        var callbackOrder = new List<string>();
        store.Add(context);

        context.Features.Get<IRaidoStatefulReconnectFeature>()!.OnReconnected(_ =>
        {
            lock (callbackOrder) callbackOrder.Add("first");
            return Task.CompletedTask;
        });
        context.Features.Get<IRaidoStatefulReconnectFeature>()!.OnReconnected(_ =>
        {
            lock (callbackOrder) callbackOrder.Add("second");
            return Task.CompletedTask;
        });

        original.Closed.Cancel();
        Assert.IsNotNull(await store.TryPrepareRebindAsync(context.ConnectionId, replacementContextOne));
        await CommitTransferAsync(replacementContextOne);

        replacementOne.Closed.Cancel();
        await WaitUntilAsync(() => context.LifecycleState == RaidoConnectionLifecycleState.Reconnecting);
        Assert.IsNotNull(await store.TryPrepareRebindAsync(context.ConnectionId, replacementContextTwo));
        await CommitTransferAsync(replacementContextTwo);

        CollectionAssert.AreEqual(new[] { "first", "second", "first", "second" }, callbackOrder);
        context.Abort();
    }

    [TestMethod]
    public async Task WriteFailureAllowsTheEnabledLogicalConnectionToRebind()
    {
        using var original = new PhysicalConnection("physical-1", outputWriter: new FailingPipeWriter());
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var replacement = CreateContext(replacementPhysical, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);

        await context.WriteAsync(new TestMessage());

        for (var i = 0; i < 100 && context.LifecycleState == RaidoConnectionLifecycleState.Connected; i++)
        {
            await Task.Delay(1);
        }

        Assert.AreEqual(RaidoConnectionLifecycleState.Reconnecting, context.LifecycleState);
        Assert.IsNotNull(await store.TryPrepareRebindAsync(context.ConnectionId, replacement));
        await CommitTransferAsync(replacement);
        Assert.AreEqual(RaidoConnectionLifecycleState.Connected, context.LifecycleState);
        context.Abort();
    }

    [TestMethod]
    public async Task SendDuringGraceFailsAndConcurrentRebindHasOneWinner()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementOne = new PhysicalConnection("physical-2");
        using var replacementTwo = new PhysicalConnection("physical-3");
        var context = CreateContext(original, statefulReconnect: true);
        context.Protocol = new WritingProtocol();
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        await Assert.ThrowsExactlyAsync<RaidoConnectionReconnectingException>(
            () => context.WriteAsync(new TestMessage()).AsTask());

        var replacementContextOne = CreateContext(replacementOne, statefulReconnect: true);
        var replacementContextTwo = CreateContext(replacementTwo, statefulReconnect: true);
        var results = await Task.WhenAll(
            store.TryPrepareRebindAsync(context.ConnectionId, replacementContextOne).AsTask(),
            store.TryPrepareRebindAsync(context.ConnectionId, replacementContextTwo).AsTask());

        Assert.AreEqual(1, results.Count(result => result is not null));
        await CommitTransferAsync(results[0] is not null ? replacementContextOne : replacementContextTwo);
        Assert.AreEqual(RaidoConnectionLifecycleState.Connected, context.LifecycleState);
        context.Abort();
    }

    [TestMethod]
    public async Task ExplicitCloseDuringGraceRejectsLaterRebind()
    {
        using var original = new PhysicalConnection("physical-1");
        using var replacementPhysical = new PhysicalConnection("physical-2");
        var context = CreateContext(original, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        context.Abort();
        await context.AbortAsync();
        store.Remove(context);
        var rebound = await store.TryPrepareRebindAsync(context.ConnectionId, CreateContext(replacementPhysical, statefulReconnect: true));

        Assert.IsNull(rebound);
        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, context.LifecycleState);
        Assert.IsNull(store[context.ConnectionId]);
    }

    [TestMethod]
    public async Task StoreDisposalClosesRetainedConnections()
    {
        using var original = new PhysicalConnection("physical-1");
        var context = CreateContext(original, statefulReconnect: true);
        var store = new RaidoConnectionStore();
        store.Add(context);
        original.Closed.Cancel();

        store.Dispose();
        await context.WaitForTerminationAsync();

        Assert.AreEqual(0, store.Count);
        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, context.LifecycleState);
    }

    [TestMethod]
    public async Task ReconnectFeatureVetoesRetentionBeforeTransportLoss()
    {
        using var physical = new PhysicalConnection("physical-1");
        var context = CreateContext(physical, statefulReconnect: true);
        var feature = context.Features.Get<IRaidoStatefulReconnectFeature>();

        Assert.IsNotNull(feature);
        feature!.DisableReconnect();
        physical.Closed.Cancel();
        await context.AbortAsync();

        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, context.LifecycleState);
    }

    [TestMethod]
    public async Task ReconnectFeatureVetoesAnActiveGraceWindow()
    {
        using var physical = new PhysicalConnection("physical-1");
        var context = CreateContext(physical, statefulReconnect: true);
        var feature = context.Features.Get<IRaidoStatefulReconnectFeature>();

        physical.Closed.Cancel();
        Assert.AreEqual(RaidoConnectionLifecycleState.Reconnecting, context.LifecycleState);

        feature!.DisableReconnect();
        await context.AbortAsync();

        Assert.AreEqual(RaidoConnectionLifecycleState.Closed, context.LifecycleState);
        Assert.IsFalse(await context.WaitForRebindOrCloseAsync());
    }

    private static RaidoConnectionContext CreateContext(
        PhysicalConnection physical,
        bool statefulReconnect,
        TimeProvider? timeProvider = null,
        bool enableReconnect = true,
        PipeOptions? applicationOutputOptions = null,
        bool startPhysicalSession = true)
    {
        var options = new RaidoConnectionContextOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1),
            ClientTimeoutInterval = TimeSpan.FromMinutes(1),
            StatefulReconnectEnabled = statefulReconnect,
            StatefulReconnectGracePeriod = TimeSpan.FromSeconds(1),
            TimeProvider = timeProvider ?? TimeProvider.System
        };
        var context = applicationOutputOptions is null
            ? new RaidoConnectionContext(physical.Context, options, NullLoggerFactory.Instance)
            : new RaidoConnectionContext(
                new RaidoApplicationConnection(applicationOutputOptions),
                new RaidoPhysicalConnectionSession(physical.Context, NullLoggerFactory.Instance),
                physical.Context.Features,
                physical.Context.Items,
                options,
                NullLoggerFactory.Instance)
        {
            Protocol = new WritingProtocol()
        };

        if (statefulReconnect && enableReconnect)
        {
            context.Features.Get<IRaidoStatefulReconnectFeature>()!.EnableReconnect();
        }

        if (startPhysicalSession) _ = context.StartPhysicalSession();
        return context;
    }

    private static async Task CommitTransferAsync(RaidoConnectionContext replacement)
    {
        var transfer = replacement.TakePendingTransfer();
        Assert.IsNotNull(transfer);
        Assert.IsTrue(await transfer!.CommitAsync(() => new ValueTask<ReadOnlyMemory<byte>>(ReadOnlyMemory<byte>.Empty)));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(1);
        }

        Assert.IsTrue(condition());
    }

    private static async Task<byte[]> ReadOutputAsync(PipeReader reader, int expectedLength)
    {
        var bytes = new List<byte>(expectedLength);
        while (bytes.Count < expectedLength)
        {
            var result = await reader.ReadAsync();
            bytes.AddRange(result.Buffer.ToArray());
            reader.AdvanceTo(result.Buffer.End);
            if (result.IsCompleted) break;
        }

        return bytes.ToArray();
    }
}
