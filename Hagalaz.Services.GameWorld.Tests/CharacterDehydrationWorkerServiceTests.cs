using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Messages.Mediator;
using AutoMapper;
using Hagalaz.Services.GameWorld.Profiles;
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
    public async Task FlushAsync_WhenPendingLogoutPersistenceSucceeds_RemovesCharacterAndPublishesWorldSignOut()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.IsDestroyed.Returns(false);
        var persistenceService = Substitute.For<ICharacterPersistenceService>();
        persistenceService.IsPendingLogout(character).Returns(true);
        var characterService = new RecordingCharacterService();
        var mediator = Substitute.For<IGameMediator>();
        var store = new SingleCharacterStore(character);

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .AddScoped<ICharacterService>(_ => characterService)
            .AddScoped(_ => mediator)
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store);

        await worker.FlushAsync(force: false, CancellationToken.None);

        await persistenceService.Received(1).PersistAsync(character, true, Arg.Any<CancellationToken>());
        Assert.AreSame(character, characterService.RemovedCharacter);
        persistenceService.Received(1).Forget(42u);
        character.Received(1).Destroy();
        mediator.Received(1).Publish(Arg.Is<WorldSignOutCommand>(message => message != null && message.MasterId == 42u));
    }

    [TestMethod]
    [Timeout(5000)]
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
                return Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
            });
        var store = new SingleCharacterStore(character);

        using var provider = new ServiceCollection()
            .AddScoped(_ => persistenceService)
            .BuildServiceProvider();
        var worker = new CharacterDehydrationWorkerService(
            NullLogger<CharacterDehydrationWorkerService>.Instance,
            provider,
            store,
            TimeSpan.FromMilliseconds(250));

        var stopTask = worker.StopAsync(CancellationToken.None);
        await persistenceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<OperationCanceledException>(() => stopTask);

        await persistenceService.Received(1).PersistAsync(character, true, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void CreateRequest_GeneratesUniqueCorrelationIdsPerCharacter()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(x => x.AddProfile<CharacterProfile>())
            .BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var first = CharacterDehydrationWorkerService.CreateRequest(mapper, new CharacterModel(), 1, 10);
        var second = CharacterDehydrationWorkerService.CreateRequest(mapper, new CharacterModel(), 2, 11);

        Assert.AreNotEqual(first.CorrelationId, second.CorrelationId);
        Assert.AreEqual(1u, first.MasterId);
        Assert.AreEqual(2u, second.MasterId);
        Assert.AreEqual(10L, first.SnapshotRevision);
        Assert.AreEqual(11L, second.SnapshotRevision);
    }

    private sealed class SingleCharacterStore : ICharacterStore
    {
        private readonly ICharacter _character;

        public SingleCharacterStore(ICharacter character) => _character = character;

        public async IAsyncEnumerable<ICharacter> FindAllAsync()
        {
            yield return _character;
            await Task.CompletedTask;
        }

        public ValueTask<int> CountAsync() => throw new System.NotImplementedException();
        public ValueTask<bool> AddAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<bool> RemoveAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<ICharacter?> FindAsync(System.Func<ICharacter, bool> predicate) => throw new System.NotImplementedException();
        public ValueTask<ICharacter?> FindByIdAsync(uint id) => throw new System.NotImplementedException();
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
        public IAsyncEnumerable<ICharacter> FindAll() => throw new System.NotImplementedException();
        public ValueTask<bool> AddAsync(ICharacter character) => throw new System.NotImplementedException();
        public ValueTask<int> CountAsync() => throw new System.NotImplementedException();
    }
}
