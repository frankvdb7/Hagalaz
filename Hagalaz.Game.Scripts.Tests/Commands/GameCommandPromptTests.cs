using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
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

    [TestMethod]
    public async Task TeletomeCommand_UsesIssuingCharacterSnapshotAfterLookup()
    {
        var lookup = new TaskCompletionSource<IReadOnlyDictionary<int, ICharacter>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<ICharacterStore>();
        store.GetSnapshotAsync().Returns(_ => new ValueTask<IReadOnlyDictionary<int, ICharacter>>(lookup.Task));
        using var services = new ServiceCollection()
            .AddSingleton<ICharacterStore>(store)
            .BuildServiceProvider();

        var character = Substitute.For<ICharacter>();
        character.Permissions.Returns(Permission.GameModerator);
        character.ServiceProvider.Returns(services);
        var initialLocation = Location.Create(3200, 3200, 0, 1);
        var currentLocation = initialLocation;
        character.Location.Returns(_ => currentLocation);
        var currentDisplayName = "Issuer";
        character.DisplayName.Returns(_ => currentDisplayName);

        var target = Substitute.For<ICharacter>();
        target.DisplayName.Returns("Target");
        target.Location.Returns(Location.Create(3300, 3300, 0, 1));
        var targetMovement = Substitute.For<IMovement>();
        target.Movement.Returns(targetMovement);
        ITaskItem? targetTask = null;
        target.QueueTask(Arg.Do<ITaskItem>(task => targetTask = task))
            .Returns(Substitute.For<IRsTaskHandle>());

        using var commandServices = new ServiceCollection()
            .AddSingleton<IEnumerable<IGameCommand>>(Array.Empty<IGameCommand>())
            .BuildServiceProvider();
        var prompt = new GameCommandPrompt(commandServices, NullLogger<GameCommandPrompt>.Instance);
        var command = prompt.ExecuteAsync("teletome", character, ["Target"]).AsTask();

        lookup.SetResult(new Dictionary<int, ICharacter> { [1] = target });
        Assert.IsTrue(await command);

        currentLocation = Location.Create(3400, 3400, 0, 1);
        currentDisplayName = "Changed issuer";
        targetTask!.Tick();

        targetMovement.Received(1).Teleport(Arg.Is<ITeleport>(teleport =>
            teleport.Location.X == initialLocation.X && teleport.Location.Y == initialLocation.Y));
        target.Received(1).SendChatMessage("You've been teleported to Issuer.");
    }

    [TestMethod]
    public async Task TeletoCommand_UsesTargetSnapshotAfterLookup()
    {
        var lookup = new TaskCompletionSource<IReadOnlyDictionary<int, ICharacter>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<ICharacterStore>();
        store.GetSnapshotAsync().Returns(_ => new ValueTask<IReadOnlyDictionary<int, ICharacter>>(lookup.Task));
        using var services = new ServiceCollection()
            .AddSingleton<ICharacterStore>(store)
            .BuildServiceProvider();

        var character = Substitute.For<ICharacter>();
        character.Permissions.Returns(Permission.GameModerator);
        character.ServiceProvider.Returns(services);
        var movement = Substitute.For<IMovement>();
        character.Movement.Returns(movement);
        ITaskItem? characterTask = null;
        character.QueueTask(Arg.Do<ITaskItem>(task => characterTask = task))
            .Returns(Substitute.For<IRsTaskHandle>());

        var target = Substitute.For<ICharacter>();
        target.DisplayName.Returns(_ => "Target");
        var initialLocation = Location.Create(3300, 3300, 0, 1);
        var currentLocation = initialLocation;
        target.Location.Returns(_ => currentLocation);

        using var commandServices = new ServiceCollection()
            .AddSingleton<IEnumerable<IGameCommand>>(Array.Empty<IGameCommand>())
            .BuildServiceProvider();
        var prompt = new GameCommandPrompt(commandServices, NullLogger<GameCommandPrompt>.Instance);
        var command = prompt.ExecuteAsync("teleto", character, ["Target"]).AsTask();

        lookup.SetResult(new Dictionary<int, ICharacter> { [1] = target });
        Assert.IsTrue(await command);

        currentLocation = Location.Create(3400, 3400, 0, 1);
        target.DisplayName.Returns("Changed target");
        characterTask!.Tick();

        movement.Received(1).Teleport(Arg.Is<ITeleport>(teleport =>
            teleport.Location.X == initialLocation.X && teleport.Location.Y == initialLocation.Y));
        character.Received(1).SendChatMessage("You teleported to Target.");
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
