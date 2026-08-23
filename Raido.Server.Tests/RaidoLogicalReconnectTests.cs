using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
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

        public PhysicalConnection(string id, IFeatureCollection? features = null)
        {
            var transport = Substitute.For<IDuplexPipe>();
            transport.Input.Returns(Input.Reader);
            transport.Output.Returns(Output.Writer);
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
        var context = CreateContext(physical, statefulReconnect: false);
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
        Assert.AreEqual(RaidoConnectionLifecycleState.Connected, context.LifecycleState);
        Assert.AreEqual(logicalId, context.ConnectionId);
        Assert.AreEqual("physical-2", context.PhysicalConnectionId);
        Assert.AreSame(features, context.Features);
        Assert.AreSame(context, store[logicalId]);
        Assert.IsFalse(context.IsCurrentPhysicalGeneration(1));

        await context.WriteAsync(new TestMessage());
        var result = await replacementPhysical.Output.Reader.ReadAsync();
        Assert.AreEqual(42, result.Buffer.FirstSpan[0]);
        replacementPhysical.Output.Reader.AdvanceTo(result.Buffer.End);

        Assert.IsFalse(original.Output.Reader.TryRead(out var oldResult));
        if (oldResult.Buffer.Length > 0)
        {
            original.Output.Reader.AdvanceTo(oldResult.Buffer.End);
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
        Assert.AreSame(replacementProtocol, context.Protocol);

        await context.WriteAsync(new TestMessage());
        var result = await replacementPhysical.Output.Reader.ReadAsync();
        Assert.AreEqual(43, result.Buffer.FirstSpan[0]);
        replacementPhysical.Output.Reader.AdvanceTo(result.Buffer.End);
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

        var results = await Task.WhenAll(
            store.TryRebindAsync(context.ConnectionId, CreateContext(replacementOne, statefulReconnect: true)).AsTask(),
            store.TryRebindAsync(context.ConnectionId, CreateContext(replacementTwo, statefulReconnect: true)).AsTask());

        Assert.AreEqual(1, results.Count(result => result));
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
        TimeProvider? timeProvider = null)
    {
        return new RaidoConnectionContext(physical.Context, new RaidoConnectionContextOptions
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
    }
}
