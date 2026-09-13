using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Commands;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands;

[TestClass]
public sealed class GameCommandPromptTests
{
    [TestMethod]
    public async Task PlayersCommand_DoesNotMessageCharacterAfterCharacterIsDestroyed()
    {
        var countRead = new TaskCompletionSource<int>();
        var characterStore = new GatedCharacterStore(countRead);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var creatureTaskService = new CreatureTaskService(scheduler);
        using var taskCancellation = new CancellationTokenSource();
        var character = Substitute.For<ICharacter>();
        character.QueueTask(Arg.Any<ITaskItem>()).Returns(callInfo =>
        {
            var queuedTask = callInfo.Arg<ITaskItem>();
            return creatureTaskService.Queue(queuedTask, taskCancellation.Token);
        });
        character.When(value => value.Destroy()).Do(_ => taskCancellation.Cancel());

        using var characterServices = new ServiceCollection()
            .AddSingleton<ICharacterStore>(characterStore)
            .BuildServiceProvider();
        using var commandServices = new ServiceCollection()
            .AddSingleton<IEnumerable<IGameCommand>>(Array.Empty<IGameCommand>())
            .BuildServiceProvider();
        var prompt = new GameCommandPrompt(commandServices, NullLogger<GameCommandPrompt>.Instance);
        character.Permissions.Returns(Permission.Standard);
        character.ServiceProvider.Returns(characterServices);
        var task = prompt.ExecuteAsync("players", character, []).AsTask();

        Assert.IsFalse(task.IsCompleted);
        character.DidNotReceive().SendChatMessage(Arg.Any<string>());

        character.Destroy();
        countRead.SetResult(3);
        Assert.IsTrue(await task);
        scheduler.Tick();

        character.DidNotReceive().SendChatMessage(Arg.Any<string>());
    }

    private sealed class GatedCharacterStore(TaskCompletionSource<int> countRead) : ICharacterStore
    {
        public ValueTask<IReadOnlyDictionary<int, ICharacter>> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<int> CountAsync() => new(countRead.Task);

        public ValueTask<bool> AddAsync(ICharacter character) => throw new NotSupportedException();

        public ValueTask<bool> RemoveAsync(ICharacter character) => throw new NotSupportedException();

        public ValueTask<ICharacter?> FindByIdAsync(uint id) => throw new NotSupportedException();

        public ValueTask<ICharacter?> FindByIndexAsync(int index) => throw new NotSupportedException();
        public ICharacter? FindByMasterId(uint id) => null;
        public bool Remove(ICharacter character) => false;
    }
}
