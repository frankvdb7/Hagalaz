using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Commands;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands;

[TestClass]
public sealed class CommandGameLoopOwnershipTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

    [TestMethod]
    public async Task SearchObjectCommand_SendsResultsOnlyOnGameWorkerContinuation()
    {
        var definitionReady = new TaskCompletionSource<IGameObjectDefinition>(TaskCreationOptions.RunContinuationsAsynchronously);
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.Name.Returns("Test Object");
        var gameObjectService = Substitute.For<IGameObjectService>();
        gameObjectService.GetObjectsCount().Returns(1);
        gameObjectService.FindGameObjectDefinitionById(0).Returns(definitionReady.Task);
        var character = CreateCharacter(out var characterStore);
        var command = new SearchObjectCommand(gameObjectService);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var creatureTaskService = new CreatureTaskService(scheduler);
        var queuedContinuation = new TaskCompletionSource<ITaskItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var taskCancellation = ConfigureCharacterQueue(character, creatureTaskService, queuedContinuation);
        var task = new RsAsyncTask(() => command.Execute(new GameCommandArgs(character, ["test"])));

        scheduler.Schedule(task);
        scheduler.Tick();

        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!);
        definitionReady.SetResult(definition);
        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!);

        scheduler.Tick();
        await queuedContinuation.Task.WaitAsync(TestTimeout);
        scheduler.Tick();

        character.Received(1).SendChatMessage("0 - Test Object", ChatMessageType.ConsoleText);
        Assert.IsTrue(task.IsCompleted);
    }

    [TestMethod]
    public async Task SearchObjectCommand_DoesNotMutateCharacterAfterCharacterIsDestroyed()
    {
        var definitionReady = new TaskCompletionSource<IGameObjectDefinition>(TaskCreationOptions.RunContinuationsAsynchronously);
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.Name.Returns("Test Object");
        var gameObjectService = Substitute.For<IGameObjectService>();
        gameObjectService.GetObjectsCount().Returns(1);
        gameObjectService.FindGameObjectDefinitionById(0).Returns(definitionReady.Task);
        var character = CreateCharacter(out var characterStore);
        var command = new SearchObjectCommand(gameObjectService);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var creatureTaskService = new CreatureTaskService(scheduler);
        var queuedContinuation = new TaskCompletionSource<ITaskItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var taskCancellation = ConfigureCharacterQueue(character, creatureTaskService, queuedContinuation);
        var task = new RsAsyncTask(() => command.Execute(new GameCommandArgs(character, ["test"])));

        scheduler.Schedule(task);
        scheduler.Tick();

        character.Destroy();
        definitionReady.SetResult(definition);
        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!);

        scheduler.Tick();
        await queuedContinuation.Task.WaitAsync(TestTimeout);
        scheduler.Tick();

        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!);
        Assert.IsTrue(task.IsCompleted);
    }

    [TestMethod]
    public async Task SpawnBoxCommand_DoesNotMutateDestroyedCharacterAfterBackgroundSearch()
    {
        var searchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSearch = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var definition = Substitute.For<IItemDefinition>();
        definition.Name.Returns("Test Item");
        var itemService = Substitute.For<IItemService>();
        itemService.GetTotalItemCount().Returns(_ =>
        {
            searchStarted.SetResult(true);
            releaseSearch.Task.Wait(TestTimeout);
            return 1;
        });
        itemService.FindItemDefinitionById(0).Returns(definition);

        var area = Substitute.For<IArea>();
        area.IsPvP.Returns(false);
        var widgets = Substitute.For<IWidgetContainer>();
        var configurations = Substitute.For<IConfigurations>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IItemService)).Returns(itemService);
        var character = CreateCharacter(out var characterStore);
        serviceProvider.GetService(typeof(ICharacterStore)).Returns(characterStore);
        character.Area.Returns(area);
        character.Widgets.Returns(widgets);
        character.Configurations.Returns(configurations);
        character.ServiceProvider.Returns(serviceProvider);
        var command = new SpawnBoxCommand(Substitute.For<Abstractions.Builders.Item.IItemBuilder>());
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var creatureTaskService = new CreatureTaskService(scheduler);
        var queuedContinuation = new TaskCompletionSource<ITaskItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var taskCancellation = ConfigureCharacterQueue(character, creatureTaskService, queuedContinuation);
        using var task = new RsAsyncTask(() => command.Execute(new GameCommandArgs(character, ["test"])));

        scheduler.Schedule(task);
        try
        {
            scheduler.Tick();
            await searchStarted.Task.WaitAsync(TestTimeout);

            character.DidNotReceiveWithAnyArgs().SendChatMessage(default!);
            widgets.DidNotReceiveWithAnyArgs().OpenWidget(default, default, default!, default);
            character.Destroy();
        }
        finally
        {
            releaseSearch.TrySetResult(true);
        }

        using var timeout = new CancellationTokenSource(TestTimeout);
        scheduler.Tick();
        while (!task.IsCompleted && !task.IsFaulted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), timeout.Token);
            scheduler.Tick();
        }
        scheduler.Tick();
        scheduler.Tick();

        Assert.IsTrue(task.IsCompleted);
        Assert.IsTrue(queuedContinuation.Task.IsCompletedSuccessfully);
        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!);
        widgets.DidNotReceiveWithAnyArgs().OpenWidget(default, default, default!, default);
        configurations.DidNotReceiveWithAnyArgs().SendCs2Script(default, default!);
        configurations.DidNotReceiveWithAnyArgs().SendItems(default, default, default!);
    }

    private static ICharacter CreateCharacter(out ICharacterStore characterStore)
    {
        characterStore = Substitute.For<ICharacterStore>();
        var character = Substitute.For<ICharacter>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICharacterStore)).Returns(characterStore);
        character.ServiceProvider.Returns(serviceProvider);
        return character;
    }

    private static CancellationTokenSource ConfigureCharacterQueue(
        ICharacter character,
        ICreatureTaskService creatureTaskService,
        TaskCompletionSource<ITaskItem>? queuedTask = null)
    {
        var taskCancellation = new CancellationTokenSource();
        character.QueueTask(Arg.Any<ITaskItem>()).Returns(callInfo =>
        {
            var task = callInfo.Arg<ITaskItem>();
            queuedTask?.TrySetResult(task);
            return creatureTaskService.Queue(task, taskCancellation.Token);
        });
        character.When(value => value.Destroy()).Do(_ => taskCancellation.Cancel());
        return taskCancellation;
    }
}
