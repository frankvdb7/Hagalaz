using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
[DoNotParallelize] // The instruments are process-wide, so concurrent services would contaminate the listener.
public sealed class CharacterPersistenceHealthMetricsTests
{
    private const string MeterName = "Hagalaz.Services.GameWorld";
    private const string SnapshotDuration = "hagalaz.gameworld.character.snapshot.duration";
    private const string FlushDuration = "hagalaz.gameworld.character.flush.duration";
    private const string Flush = "hagalaz.gameworld.character.flush";
    private const string Recovery = "hagalaz.gameworld.character.logout.recovery";

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_Periodic_RecordsSnapshotAndSuccessfulFlush()
    {
        var harness = new FlushHarness();
        using var spy = new MeterListenerSpy(MeterName);

        await harness.Worker.FlushAsync(force: false, CancellationToken.None);

        AssertFlush(spy, "periodic", "success");
        Assert.AreEqual("success", Single(spy, SnapshotDuration).Tags["outcome"]);
        Assert.AreEqual("s", Single(spy, SnapshotDuration).Unit);
        AssertBoundedTags(spy);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_PeriodicPartialFailure_IsNotReportedAsSuccess()
    {
        var harness = new FlushHarness();
        harness.Persistence.PersistAsync(42, Arg.Any<CharacterModel>(), false, Arg.Any<CancellationToken>())
            .Returns<Task<CharacterPersistenceReceipt?>>(_ => throw new InvalidOperationException("outbox"));
        using var spy = new MeterListenerSpy(MeterName);

        await harness.Worker.FlushAsync(force: false, CancellationToken.None);

        AssertFlush(spy, "periodic", "failure");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_ShutdownPartialFailure_RecordsFailureAndStillThrows()
    {
        var harness = new FlushHarness();
        harness.Persistence.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns<Task<CharacterPersistenceReceipt?>>(_ => throw new InvalidOperationException("outbox"));
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsAsync<AggregateException>(() => harness.Worker.FlushAsync(force: true, CancellationToken.None));

        AssertFlush(spy, "shutdown", "failure");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_WhenCancelledWhileHandingOff_RecordsCancelledFlush()
    {
        var harness = new FlushHarness();
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Persistence.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
                return (CharacterPersistenceReceipt?)null;
            });
        using var spy = new MeterListenerSpy(MeterName);

        var flush = harness.Worker.FlushAsync(force: true, cancellation.Token);
        await started.Task;
        cancellation.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(() => flush);

        AssertFlush(spy, "shutdown", "cancelled");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_WhenSnapshotCaptureFails_RecordsFailedSnapshotAndFlush()
    {
        var harness = new FlushHarness();
        harness.Dehydration.Dehydrate(harness.Character).Returns(_ => throw new InvalidOperationException("dehydrate"));
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Worker.FlushAsync(force: false, CancellationToken.None));

        Assert.AreEqual("failure", Single(spy, SnapshotDuration).Tags["outcome"]);
        AssertFlush(spy, "periodic", "failure");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_WhenLogoutRecoveryFails_RecordsFailureWithoutSnapshot()
    {
        var harness = new FlushHarness();
        harness.Logout.RecoverPendingLogoutsAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("recovery"));
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Worker.FlushAsync(force: false, CancellationToken.None));

        Assert.IsFalse(spy.Measurements.Any(item => item.InstrumentName == SnapshotDuration));
        AssertFlush(spy, "periodic", "failure");
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_WhenLogoutCompletes_CountsCompleted()
    {
        var fixture = RecoveryFixture.Create(CharacterPersistenceOutcome.Committed);
        using var spy = new MeterListenerSpy(MeterName);

        await fixture.Service.RecoverPendingLogoutsAsync();

        Assert.AreEqual("completed", Single(spy, Recovery).Tags["outcome"]);
        Assert.AreEqual(1, Single(spy, Recovery).Value);
        AssertBoundedTags(spy);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_WhenPersistenceIsNotCommitted_CountsIncompleteNotCompleted()
    {
        var fixture = RecoveryFixture.Create(CharacterPersistenceOutcome.Conflict);
        using var spy = new MeterListenerSpy(MeterName);

        await fixture.Service.RecoverPendingLogoutsAsync();

        Assert.AreEqual("incomplete", Single(spy, Recovery).Tags["outcome"]);
        Assert.IsTrue(fixture.State.IsPending(fixture.Character));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_WhenSessionRemovalFails_CountsIncomplete()
    {
        var fixture = RecoveryFixture.Create(CharacterPersistenceOutcome.Committed, sessionRemoved: false);
        using var spy = new MeterListenerSpy(MeterName);

        await fixture.Service.RecoverPendingLogoutsAsync();

        Assert.AreEqual("incomplete", Single(spy, Recovery).Tags["outcome"]);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_WhenPersistenceThrows_CountsFailed()
    {
        var fixture = RecoveryFixture.Create(CharacterPersistenceOutcome.Committed);
        fixture.Persistence.WaitForAcknowledgementAsync(Arg.Any<CharacterPersistenceReceipt>(), Arg.Any<CancellationToken>())
            .Returns<Task<CharacterPersistenceOutcome>>(_ => throw new InvalidOperationException("ack"));
        using var spy = new MeterListenerSpy(MeterName);

        await fixture.Service.RecoverPendingLogoutsAsync();

        Assert.AreEqual("failed", Single(spy, Recovery).Tags["outcome"]);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_WhenCancelled_CountsCancelled()
    {
        var fixture = RecoveryFixture.Create(CharacterPersistenceOutcome.Committed);
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Persistence.WaitForAcknowledgementAsync(Arg.Any<CharacterPersistenceReceipt>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
                return CharacterPersistenceOutcome.Committed;
            });
        using var spy = new MeterListenerSpy(MeterName);

        var recovery = fixture.Service.RecoverPendingLogoutsAsync(cancellation.Token);
        await started.Task;
        cancellation.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(() => recovery);

        Assert.AreEqual("cancelled", Single(spy, Recovery).Tags["outcome"]);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_WithNothingToRecover_RecordsNothing()
    {
        var fixture = RecoveryFixture.Create(CharacterPersistenceOutcome.Committed, eligible: false);
        using var spy = new MeterListenerSpy(MeterName);

        await fixture.Service.RecoverPendingLogoutsAsync();

        Assert.IsFalse(spy.Measurements.Any(item => item.InstrumentName == Recovery));
    }

    private static MetricMeasurement Single(MeterListenerSpy spy, string instrument) =>
        spy.Measurements.Single(item => item.InstrumentName == instrument);

    private static void AssertFlush(MeterListenerSpy spy, string mode, string outcome)
    {
        var counter = Single(spy, Flush);
        var duration = Single(spy, FlushDuration);
        Assert.AreEqual(1, counter.Value);
        Assert.AreEqual(mode, counter.Tags["mode"]);
        Assert.AreEqual(outcome, counter.Tags["outcome"]);
        Assert.AreEqual(mode, duration.Tags["mode"]);
        Assert.AreEqual(outcome, duration.Tags["outcome"]);
        Assert.AreEqual("s", duration.Unit);
    }

    private static void AssertBoundedTags(MeterListenerSpy spy)
    {
        var allowed = new HashSet<string> { "mode", "outcome" };
        Assert.IsTrue(spy.Measurements
            .Where(item => item.InstrumentName.StartsWith("hagalaz.gameworld.character.", StringComparison.Ordinal))
            .All(item => item.Tags.Keys.All(allowed.Contains)));
    }

    private sealed class FlushHarness
    {
        public FlushHarness()
        {
            Character = Substitute.For<ICharacter>();
            Character.MasterId.Returns(42u);
            Persistence = Substitute.For<ICharacterPersistenceService>();
            Persistence.PersistAsync(Arg.Any<uint>(), Arg.Any<CharacterModel>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<CharacterPersistenceReceipt?>(null));
            Dehydration = Substitute.For<ICharacterDehydrationService>();
            Dehydration.Dehydrate(Character).Returns(new CharacterModel());
            Logout = Substitute.For<ICharacterLogoutService>();
            Logout.IsPendingLogout(42u).Returns(false);
            var provider = new ServiceCollection()
                .AddScoped(_ => Persistence)
                .AddScoped(_ => Dehydration)
                .AddScoped(_ => Logout)
                .AddSingleton(new CharacterPersistenceState())
                .BuildServiceProvider();
            Worker = new CharacterDehydrationWorkerService(
                NullLogger<CharacterDehydrationWorkerService>.Instance,
                provider,
                new SingleCharacterStore(Character),
                new InlineTaskScheduler());
        }

        public ICharacter Character { get; }
        public ICharacterPersistenceService Persistence { get; }
        public ICharacterDehydrationService Dehydration { get; }
        public ICharacterLogoutService Logout { get; }
        public CharacterDehydrationWorkerService Worker { get; }
    }

    private sealed class RecoveryFixture
    {
        public required ICharacter Character { get; init; }
        public required CharacterLogoutState State { get; init; }
        public required ICharacterPersistenceService Persistence { get; init; }
        public required CharacterLogoutService Service { get; init; }

        public static RecoveryFixture Create(
            CharacterPersistenceOutcome outcome,
            bool sessionRemoved = true,
            bool eligible = true)
        {
            var character = Substitute.For<ICharacter>();
            character.MasterId.Returns(42u);
            var session = Substitute.For<IGameSession>();
            session.ConnectionId.Returns("connection");
            session.SessionGeneration.Returns(7L);
            character.Session.Returns(session);

            var state = new CharacterLogoutState();
            Assert.IsTrue(state.TryBeginLogout(character, out _));
            Assert.IsTrue(state.SetSnapshot(character, new CharacterModel { SnapshotRevision = 1 }));
            if (eligible)
            {
                Assert.IsTrue(state.MarkRecoveryEligible(character));
            }

            var persistenceState = new CharacterPersistenceState();
            persistenceState.InitializeRevision(42, 0, 7);
            var receipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 1);
            var persistence = Substitute.For<ICharacterPersistenceService>();
            persistence.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<CharacterPersistenceReceipt?>(receipt));
            persistence.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(outcome));
            var sessions = Substitute.For<IGameSessionService>();
            sessions.RemoveSession(session, Arg.Any<CancellationToken>()).Returns(Task.FromResult(sessionRemoved));

            return new RecoveryFixture
            {
                Character = character,
                State = state,
                Persistence = persistence,
                Service = new CharacterLogoutService(
                    state,
                    Substitute.For<ICharacterService>(),
                    new InlineTaskScheduler(),
                    Substitute.For<IGameMediator>(),
                    persistenceState,
                    persistence,
                    sessions)
            };
        }
    }

    private sealed class InlineTaskScheduler : IRsTaskService
    {
        public void Schedule(ITaskItem action) => action.Tick();
        public void Tick() { }
    }

    private sealed class SingleCharacterStore(ICharacter character) : ICharacterStore
    {
        public ValueTask<IReadOnlyDictionary<int, ICharacter>> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            new(new Dictionary<int, ICharacter> { [character.Index] = character });

        public ValueTask<int> CountAsync() => throw new NotImplementedException();
        public ValueTask<bool> AddAsync(ICharacter value) => throw new NotImplementedException();
        public ValueTask<bool> RemoveAsync(ICharacter value) => throw new NotImplementedException();
        public ValueTask<ICharacter?> FindByIdAsync(uint id) => throw new NotImplementedException();
        public ValueTask<ICharacter?> FindByIndexAsync(int index) => throw new NotImplementedException();
        public ICharacter? FindByMasterId(uint id) => id == character.MasterId ? character : null;
        public bool Remove(ICharacter value) => false;
    }
}
