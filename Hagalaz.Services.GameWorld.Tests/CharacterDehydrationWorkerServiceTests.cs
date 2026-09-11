using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Services;
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
        var characterService = new RecordingCharacterService();
        var mediator = Substitute.For<IGameMediator>();
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        characterLogoutService.IsPendingLogout(character).Returns(true);
        var store = new SingleCharacterStore(character);

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped<ICharacterService>(_ => characterService)
            .AddScoped(_ => mediator)
            .AddScoped(_ => characterLogoutService)
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store);

        await worker.FlushAsync(force: false, CancellationToken.None);

        await persistenceService.DidNotReceive().PersistAsync(character, false, Arg.Any<CancellationToken>());
        await characterLogoutService.DidNotReceive().DetachAsync(character, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task FlushAsync_WhenLogoutIsNotPending_PersistsCharacter()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.PersistAsync(character, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CharacterPersistenceReceipt?>(null));
        var characterService = new RecordingCharacterService();
        var mediator = Substitute.For<IGameMediator>();
        var characterLogoutService = Substitute.For<ICharacterLogoutService>();
        characterLogoutService.IsPendingLogout(character).Returns(false);
        var store = new SingleCharacterStore(character);

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped<ICharacterService>(_ => characterService)
            .AddScoped(_ => mediator)
            .AddScoped<ICharacterLogoutService>(_ => characterLogoutService)
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store);

        await worker.FlushAsync(force: false, CancellationToken.None);

        await persistenceService.Received(1).PersistAsync(character, false, Arg.Any<CancellationToken>());
        Assert.IsNull(characterService.RemovedCharacter);
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task StopAsync_WhenDurableHandoffExceedsDeadline_CancelsAndReportsFailure()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        var persistenceStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        persistenceService.PersistAsync(character, true, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                persistenceStarted.TrySetResult(true);
                return BlockPersistenceAsync(callInfo.Arg<CancellationToken>());
            });
        var store = new SingleCharacterStore(character);

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped<ICharacterLogoutService>(_ => Substitute.For<ICharacterLogoutService>())
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store,
            TimeSpan.FromSeconds(5));

        var stopTask = worker.StopAsync(CancellationToken.None);
        await persistenceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<OperationCanceledException>(() => stopTask);

        await persistenceService.Received(1).PersistAsync(character, true, Arg.Any<CancellationToken>());
    }

    private sealed class SingleCharacterStore : ICharacterStore
    {
        private readonly ICharacter _character;

        public SingleCharacterStore(ICharacter character) => _character = character;

        public ValueTask<IReadOnlyDictionary<int, ICharacter>> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            new(new Dictionary<int, ICharacter> { [_character.Index] = _character });

        public ValueTask<int> CountAsync() => throw new System.NotImplementedException();
        public ValueTask<bool> AddAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<bool> RemoveAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<ICharacter?> FindByIdAsync(uint id) => throw new System.NotImplementedException();
        public ValueTask<ICharacter?> FindByIndexAsync(int index) => throw new System.NotImplementedException();
    }

    private static async Task<CharacterPersistenceReceipt?> BlockPersistenceAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return null;
    }

    private sealed class RecordingCharacterService : ICharacterService
    {
        public ICharacter? RemovedCharacter { get; private set; }

        public ValueTask<bool> RemoveAsync(ICharacter character)
        {
            RemovedCharacter = character;
            return new ValueTask<bool>(true);
        }

        public ValueTask<ICharacter?> FindByMasterId(uint masterId) => throw new System.NotImplementedException();
        public ValueTask<ICharacter?> FindByIndex(int index) => throw new System.NotImplementedException();
        public ValueTask<bool> AddAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<int> CountAsync() => throw new System.NotImplementedException();
    }
}
