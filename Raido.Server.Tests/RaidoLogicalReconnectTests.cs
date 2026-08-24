using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
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

        public WritingProtocol(byte value = 42) => _value = value;

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
            output.GetSpan(1)[0] = _value;
            output.Advance(1);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => new[] { _value };

        public bool IsVersionSupported(int version) => version == 1;
    }

    private sealed class PhysicalConnection : IDisposable
    {
        public readonly CancellationTokenSource Closed = new();
        public readonly Pipe Input = new();
        public readonly Pipe Output = new();
        public readonly ConnectionContext Context;

        public PhysicalConnection(string id, IFeatureCollection? features = null, PipeWriter? outputWriter = null)
        {
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
        var rebound = await store.TryRebindAsync(logicalId, replacement);

        Assert.IsTrue(rebound);
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

        var rebound = await store.TryRebindAsync(context.ConnectionId, replacement, replacementProtocol);

        Assert.IsTrue(rebound);
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

        Assert.IsTrue(await store.TryRebindAsync(context.ConnectionId, replacement));
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
        var callbackCount = 0;
        store.Add(context);

        context.Features.Get<IRaidoStatefulReconnectFeature>()!.OnReconnected(_ =>
        {
            Interlocked.Increment(ref callbackCount);
            return Task.CompletedTask;
        });

        original.Closed.Cancel();
        Assert.IsTrue(await store.TryRebindAsync(context.ConnectionId, replacementContextOne));
        await CommitTransferAsync(replacementContextOne);

        replacementOne.Closed.Cancel();
        await WaitUntilAsync(() => context.LifecycleState == RaidoConnectionLifecycleState.Reconnecting);
        Assert.IsTrue(await store.TryRebindAsync(context.ConnectionId, replacementContextTwo));
        await CommitTransferAsync(replacementContextTwo);

        Assert.AreEqual(2, callbackCount);
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
        Assert.IsTrue(await store.TryRebindAsync(context.ConnectionId, replacement));
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
            store.TryRebindAsync(context.ConnectionId, replacementContextOne).AsTask(),
            store.TryRebindAsync(context.ConnectionId, replacementContextTwo).AsTask());

        Assert.AreEqual(1, results.Count(result => result));
        await CommitTransferAsync(results[0] ? replacementContextOne : replacementContextTwo);
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
        var rebound = await store.TryRebindAsync(context.ConnectionId, CreateContext(replacementPhysical, statefulReconnect: true));

        Assert.IsFalse(rebound);
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
        bool enableReconnect = true)
    {
        var context = new RaidoConnectionContext(physical.Context, new RaidoConnectionContextOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1),
            ClientTimeoutInterval = TimeSpan.FromMinutes(1),
            StatefulReconnectEnabled = statefulReconnect,
            StatefulReconnectGracePeriod = TimeSpan.FromSeconds(1),
            TimeProvider = timeProvider ?? TimeProvider.System
        }, NullLoggerFactory.Instance)
        {
            Protocol = new WritingProtocol()
        };

        if (statefulReconnect && enableReconnect)
        {
            context.Features.Get<IRaidoStatefulReconnectFeature>()!.EnableReconnect();
        }

        return context;
    }

    private static async Task CommitTransferAsync(RaidoConnectionContext replacement)
    {
        var transfer = replacement.TakePendingTransfer();
        Assert.IsNotNull(transfer);
        Assert.IsTrue(await transfer!.CommitAsync(ReadOnlySequence<byte>.Empty));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(1);
        }

        Assert.IsTrue(condition());
    }
}
