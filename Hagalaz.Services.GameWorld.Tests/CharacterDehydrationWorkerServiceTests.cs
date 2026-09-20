using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterDehydrationWorkerServiceTests
{
    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_WhenLogoutIsPending_SkipsBackgroundPersistenceAndDetach()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        var mediator = Substitute.For<IGameMediator>();
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        characterLogoutService.IsPendingLogout(character).Returns(true);
        characterLogoutService.IsPendingLogout(42u).Returns(true);
        var store = new SingleCharacterStore(character);

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped(_ => dehydrationService)
            .AddScoped(_ => mediator)
            .AddScoped(_ => characterLogoutService)
            .AddSingleton(new CharacterPersistenceState())
            .BuildServiceProvider();
        var scheduler = new ImmediateTaskScheduler();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store,
            scheduler);

        await worker.FlushAsync(force: false, CancellationToken.None);

        await persistenceService.DidNotReceive().PersistAsync(42, Arg.Any<CharacterModel>(), false, Arg.Any<CancellationToken>());
        await characterLogoutService.DidNotReceive().DetachAsync(character, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_WhenLogoutIsNotPending_PersistsCharacter()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.PersistAsync(42, Arg.Any<CharacterModel>(), false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CharacterPersistenceReceipt?>(null));
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        var mediator = Substitute.For<IGameMediator>();
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        characterLogoutService.IsPendingLogout(character).Returns(false);
        var store = new SingleCharacterStore(character);

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped(_ => dehydrationService)
            .AddScoped(_ => mediator)
            .AddScoped<ICharacterLogoutService>(_ => characterLogoutService)
            .AddSingleton(new CharacterPersistenceState())
            .BuildServiceProvider();
        var scheduler = new ImmediateTaskScheduler();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store,
            scheduler);

        await worker.FlushAsync(force: false, CancellationToken.None);

        await persistenceService.Received(1).PersistAsync(
            42,
            Arg.Is<CharacterModel>(model => model.SnapshotRevision == 1),
            false,
            Arg.Any<CancellationToken>());
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task FlushAsync_CapturesStoreMembershipOnGameWorkerBeforeDehydrating()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        characterLogoutService.IsPendingLogout(42u).Returns(false);
        var store = new SingleCharacterStore(character);
        var scheduler = new DeferredTaskScheduler();

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped(_ => dehydrationService)
            .AddScoped<ICharacterLogoutService>(_ => characterLogoutService)
            .AddSingleton(new CharacterPersistenceState())
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store,
            scheduler);

        var flush = worker.FlushAsync(force: false, CancellationToken.None);
        await scheduler.Scheduled.Task;
        store.IncludeCharacter = false;
        scheduler.Tick();

        await flush;

        dehydrationService.DidNotReceive().Dehydrate(character);
        await persistenceService.DidNotReceive().PersistAsync(
            42,
            Arg.Any<CharacterModel>(),
            false,
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task StopAsync_WhenDurableHandoffExceedsDeadline_CancelsAndReportsFailure()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var persistenceStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        persistenceService.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                persistenceStarted.TrySetResult(true);
                return BlockPersistenceAsync(callInfo.Arg<CancellationToken>());
            });
        var store = new SingleCharacterStore(character);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped(_ => dehydrationService)
            .AddScoped<ICharacterLogoutService>(_ => Substitute.For<ICharacterLogoutService>())
            .AddSingleton(new CharacterPersistenceState())
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store,
            new ImmediateTaskScheduler(),
            TimeSpan.FromSeconds(5));

        var stopTask = worker.StopAsync(CancellationToken.None);
        await persistenceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<OperationCanceledException>(() => stopTask);

        await persistenceService.Received(1).PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>());
    }

    private sealed class SingleCharacterStore : ICharacterStore
    {
        private readonly ICharacter _character;

        public bool IncludeCharacter { get; set; } = true;

        public SingleCharacterStore(ICharacter character) => _character = character;

        public ValueTask<IReadOnlyDictionary<int, ICharacter>> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            new(IncludeCharacter
                ? new Dictionary<int, ICharacter> { [_character.Index] = _character }
                : new Dictionary<int, ICharacter>());

        public ValueTask<int> CountAsync() => throw new System.NotImplementedException();
        public ValueTask<bool> AddAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<bool> RemoveAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<ICharacter?> FindByIdAsync(uint id) => throw new System.NotImplementedException();
        public ValueTask<ICharacter?> FindByIndexAsync(int index) => throw new System.NotImplementedException();
        public ICharacter? FindByMasterId(uint id) => id == _character.MasterId ? _character : null;
        public bool Remove(ICharacter character) => false;
    }

    private sealed class DeferredTaskScheduler : IRsTaskService
    {
        public TaskCompletionSource<bool> Scheduled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private ITaskItem? _pending;

        public void Schedule(ITaskItem action)
        {
            _pending = action;
            Scheduled.TrySetResult(true);
        }

        public void ScheduleLifecycleCritical(System.Action action) => action();
        public void BeginShutdown() { }
        public void CompleteShutdown() { }

        public void Tick()
        {
            var pending = _pending ?? throw new InvalidOperationException("No task was scheduled.");
            _pending = null;
            pending.Tick();
        }
    }

    private static async Task<CharacterPersistenceReceipt?> BlockPersistenceAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return null;
    }

    private sealed class ImmediateTaskScheduler : IRsTaskService
    {
        public void Schedule(ITaskItem action) => action.Tick();
        public void Tick() { }
        public void ScheduleLifecycleCritical(System.Action action) => action();
        public void BeginShutdown() { }
        public void CompleteShutdown() { }
    }
}
