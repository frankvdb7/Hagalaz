using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands;

[TestClass]
public sealed class GameCommandPromptTests
{
    [TestMethod]
    public async Task PlayersCommand_DoesNotMessageDestroyedCharacterAfterStoreAwait()
    {
        var countRead = new TaskCompletionSource<int>();
        var characterStore = new GatedCharacterStore(countRead);

        using var characterServices = new ServiceCollection()
            .AddSingleton<ICharacterStore>(characterStore)
            .BuildServiceProvider();
        using var commandServices = new ServiceCollection()
            .AddSingleton<IEnumerable<IGameCommand>>(Array.Empty<IGameCommand>())
            .BuildServiceProvider();
        var prompt = new GameCommandPrompt(commandServices, NullLogger<GameCommandPrompt>.Instance);
        var character = Substitute.For<ICharacter>();
        var destroyed = false;
        character.IsDestroyed.Returns(_ => destroyed);
        character.Permissions.Returns(Permission.Standard);
        character.ServiceProvider.Returns(characterServices);
        var task = prompt.ExecuteAsync("players", character, []).AsTask();

        Assert.IsFalse(task.IsCompleted);
        character.DidNotReceive().SendChatMessage(Arg.Any<string>());

        destroyed = true;
        countRead.SetResult(3);
        Assert.IsTrue(await task);

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
    }
}
