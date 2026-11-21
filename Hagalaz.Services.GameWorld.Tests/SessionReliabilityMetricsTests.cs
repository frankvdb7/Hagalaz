using System.Threading;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

// The instruments are process-wide, so concurrent tests would contaminate the listener.
[TestClass]
[DoNotParallelize]
public sealed class SessionReliabilityMetricsTests
{
    private const string MeterName = "Hagalaz.Services.GameWorld";
    private const string Renew = "hagalaz.gameworld.session.lease.renew";
    private const string Pending = "hagalaz.gameworld.session.cleanup.pending";

    [TestMethod]
    [Timeout(5000)]
    public async Task Renew_WhenClaimIsRenewed_RecordsSuccess()
    {
        var (claims, lease, _) = await CreateLeaseAsync();
        claims.RenewAsync(42, "claim", Arg.Any<CancellationToken>()).Returns(true);
        using var spy = new MeterListenerSpy(MeterName);

        await lease.RenewSessionsAsync(CancellationToken.None);

        AssertSingleRenewal(spy, "success");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task Renew_WhenClaimIsLost_RecordsLostOnce()
    {
        var (claims, lease, _) = await CreateLeaseAsync();
        claims.RenewAsync(42, "claim", Arg.Any<CancellationToken>()).Returns(false);
        using var spy = new MeterListenerSpy(MeterName);

        await lease.RenewSessionsAsync(CancellationToken.None);

        AssertSingleRenewal(spy, "lost");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task Renew_WhenTransientErrorBeforeLeaseExpiry_RecordsErrorNotLost()
    {
        var (claims, lease, session) = await CreateLeaseAsync();
        session.ClaimLeaseValidUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        claims.RenewAsync(42, "claim", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("claim store unavailable")));
        using var spy = new MeterListenerSpy(MeterName);

        await lease.RenewSessionsAsync(CancellationToken.None);

        AssertSingleRenewal(spy, "error");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task Renew_WhenCancelledByShutdown_RecordsCancelled()
    {
        var (claims, lease, _) = await CreateLeaseAsync();
        using var cancellation = new CancellationTokenSource();
        claims.RenewAsync(42, "claim", Arg.Any<CancellationToken>()).Returns<Task<bool>>(_ =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsAsync<OperationCanceledException>(() => lease.RenewSessionsAsync(cancellation.Token));

        AssertSingleRenewal(spy, "cancelled");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task Renew_SkipsPendingWorldReservation_WithoutRecording()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var world = Substitute.For<IGameWorldSession>();
        world.ConnectionId.Returns("world");
        world.MasterId.Returns(42u);
        world.SessionClaimId.Returns("claim");
        Assert.IsTrue(await store.TryReserveWorldSession(world, "previous-claim"));
        var lease = GameSessionTestDependencies.CreateLeaseService(
            store, store, claims, Substitute.For<IGameSessionConnectionTerminator>());
        using var spy = new MeterListenerSpy(MeterName);

        await lease.RenewSessionsAsync(CancellationToken.None);

        Assert.AreEqual(0, spy.Measurements.Count(m => m.InstrumentName == Renew));
        await claims.DidNotReceive().RenewAsync(Arg.Any<uint>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PendingCleanupGauge_FollowsClaimReleaseTransitions()
    {
        var store = new GameSessionStore();
        using var spy = new MeterListenerSpy(MeterName);
        var baseline = ReadPending(spy, "claim_release");
        var session = CreateSession(42, "connection", "claim");
        Assert.IsTrue(await store.TryAdd(session));

        Assert.IsTrue(await store.TryMoveToPendingClaimCleanup(session));
        Assert.AreEqual(baseline + 1, ReadPending(spy, "claim_release"));

        // Idempotent re-entry does not double count.
        Assert.IsTrue(await store.TryMoveToPendingClaimCleanup(session));
        Assert.IsTrue(await store.TryAddPendingClaimCleanup(session));
        Assert.AreEqual(baseline + 1, ReadPending(spy, "claim_release"));

        Assert.IsTrue(await store.TryRemovePendingSessionCleanup(session));
        Assert.IsFalse(await store.TryRemovePendingSessionCleanup(session));
        Assert.AreEqual(baseline, ReadPending(spy, "claim_release"));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PendingCleanupGauge_FollowsAbortTransitionsThroughRetry()
    {
        var store = new GameSessionStore();
        using var spy = new MeterListenerSpy(MeterName);
        var baseline = ReadPending(spy, "abort");
        var session = CreateSession(42, "connection", "claim");
        Assert.IsTrue(await store.TryAdd(session));

        Assert.IsTrue(await store.TryMoveToPendingAbort(session));
        Assert.IsFalse(await store.TryMoveToPendingAbort(session));
        Assert.AreEqual(baseline + 1, ReadPending(spy, "abort"));

        // A failed attempt is released for retry and stays outstanding.
        Assert.IsTrue(await store.TryBeginPendingSessionAbort(session));
        Assert.IsTrue(await store.TryReleasePendingSessionAbort(session));
        Assert.AreEqual(baseline + 1, ReadPending(spy, "abort"));

        Assert.IsTrue(await store.TryRemovePendingSessionAbort(session));
        Assert.IsFalse(await store.TryRemovePendingSessionAbort(session));
        Assert.AreEqual(baseline, ReadPending(spy, "abort"));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task PendingCleanupGauge_CountsReplacedLobbySessionAbortOnCommit()
    {
        var store = new GameSessionStore();
        using var spy = new MeterListenerSpy(MeterName);
        var baseline = ReadPending(spy, "abort");
        var lobby = CreateSession(42, "lobby", "lobby-claim");
        var world = Substitute.For<IGameWorldSession>();
        world.ConnectionId.Returns("world");
        world.MasterId.Returns(42u);
        world.SessionClaimId.Returns("world-claim");
        Assert.IsTrue(await store.TryAdd(lobby));
        Assert.IsTrue(await store.TryReserveWorldSession(world, null));

        var (committed, replaced) = await store.TryCommitWorldSession(world);

        Assert.IsTrue(committed);
        Assert.AreSame(lobby, replaced);
        Assert.AreEqual(baseline + 1, ReadPending(spy, "abort"));
        Assert.IsTrue(await store.TryRemovePendingSessionAbort(lobby));
        Assert.AreEqual(baseline, ReadPending(spy, "abort"));
    }

    [TestMethod]
    [Timeout(5000)]
    public void PendingCleanupGauge_IsBoundedAndUnitsAreSessions()
    {
        using var spy = new MeterListenerSpy(MeterName);

        var observed = spy.ObserveGaugeMeasurements().Where(m => m.InstrumentName == Pending).ToArray();

        Assert.AreEqual(2, observed.Length);
        CollectionAssert.AreEquivalent(new[] { "claim_release", "abort" }, observed.Select(m => (string)m.Tags["kind"]!).ToArray());
        Assert.IsTrue(observed.All(m => m.Unit == "{session}" && m.Tags.Count == 1));
    }

    private static async Task<(IGameSessionClaimStore Claims, GameSessionLeaseService Lease, IGameSession Session)> CreateLeaseAsync()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var session = CreateSession(42, "connection", "claim");
        Assert.IsTrue(await store.TryAdd(session));
        var lease = GameSessionTestDependencies.CreateLeaseService(
            store, store, claims, Substitute.For<IGameSessionConnectionTerminator>());
        return (claims, lease, session);
    }

    private static IGameSession CreateSession(uint masterId, string connectionId, string claimId)
    {
        var session = Substitute.For<IGameSession>();
        session.MasterId.Returns(masterId);
        session.ConnectionId.Returns(connectionId);
        session.SessionClaimId.Returns(claimId);
        return session;
    }

    private static double ReadPending(MeterListenerSpy spy, string kind) =>
        spy.ObserveGaugeMeasurements().Single(m => m.InstrumentName == Pending && (string)m.Tags["kind"]! == kind).Value;

    private static void AssertSingleRenewal(MeterListenerSpy spy, string outcome)
    {
        var renewals = spy.Measurements.Where(m => m.InstrumentName == Renew).ToArray();
        Assert.AreEqual(1, renewals.Length);
        Assert.AreEqual(1d, renewals[0].Value);
        Assert.AreEqual("{renewal}", renewals[0].Unit);
        Assert.AreEqual(1, renewals[0].Tags.Count);
        Assert.AreEqual(outcome, renewals[0].Tags["outcome"]);
    }
}
