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
    public async Task PlayersCommand_DoesNotMessageCharacterAfterOwnershipIsRevoked()
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
        character.Permissions.Returns(Permission.Standard);
        character.ServiceProvider.Returns(characterServices);
        characterStore.SetCurrent(character);
        var task = prompt.ExecuteAsync("players", character, []).AsTask();

        Assert.IsFalse(task.IsCompleted);
        character.DidNotReceive().SendChatMessage(Arg.Any<string>());

        characterStore.Revoke(character);
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
        private ICharacter? _current;
        public ICharacter? FindByMasterId(uint id) => _current;
        public bool IsCurrent(ICharacter character) => ReferenceEquals(_current, character);
        public bool Remove(ICharacter character) => false;
        public bool TryQueueTask(ICharacter character, Hagalaz.Game.Abstractions.Tasks.ITaskItem task) => false;
        public void SetCurrent(ICharacter character) => _current = character;
        public void Revoke(ICharacter character) { if (ReferenceEquals(_current, character)) _current = null; }
    }
}
