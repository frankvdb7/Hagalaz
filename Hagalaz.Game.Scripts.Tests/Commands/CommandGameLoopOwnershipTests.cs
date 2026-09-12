using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Commands;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands;

[TestClass]
public sealed class CommandGameLoopOwnershipTests
{
    [TestMethod]
    public async Task SearchObjectCommand_SendsResultsOnlyOnGameWorkerContinuation()
    {
        var definitionReady = new TaskCompletionSource<IGameObjectDefinition>(TaskCreationOptions.RunContinuationsAsynchronously);
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.Name.Returns("Test Object");
        var gameObjectService = Substitute.For<IGameObjectService>();
        gameObjectService.GetObjectsCount().Returns(1);
        gameObjectService.FindGameObjectDefinitionById(0).Returns(definitionReady.Task);
        var character = CreateCharacter();
        var command = new SearchObjectCommand(gameObjectService);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var task = new RsAsyncTask(() => command.Execute(new GameCommandArgs(character, ["test"])));

        scheduler.Schedule(task);
        scheduler.Tick();

        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!, default, default, default);
        definitionReady.SetResult(definition);
        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!, default, default, default);

        scheduler.Tick();

        character.Received(1).SendChatMessage("0 - Test Object", ChatMessageType.ConsoleText);
        Assert.IsTrue(task.IsCompleted);
    }

    [TestMethod]
    public async Task SearchObjectCommand_DoesNotMutateDestroyedCharacterAfterLookup()
    {
        var definitionReady = new TaskCompletionSource<IGameObjectDefinition>(TaskCreationOptions.RunContinuationsAsynchronously);
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.Name.Returns("Test Object");
        var gameObjectService = Substitute.For<IGameObjectService>();
        gameObjectService.GetObjectsCount().Returns(1);
        gameObjectService.FindGameObjectDefinitionById(0).Returns(definitionReady.Task);
        var character = CreateCharacter();
        var destroyed = false;
        character.IsDestroyed.Returns(_ => destroyed);
        var command = new SearchObjectCommand(gameObjectService);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var task = new RsAsyncTask(() => command.Execute(new GameCommandArgs(character, ["test"])));

        scheduler.Schedule(task);
        scheduler.Tick();

        destroyed = true;
        definitionReady.SetResult(definition);
        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!, default, default, default);

        scheduler.Tick();

        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!, default, default, default);
        Assert.IsTrue(task.IsCompleted);
    }

    [TestMethod]
    public async Task SpawnBoxCommand_DoesNotMutateDestroyedCharacterAfterBackgroundSearch()
    {
        var searchStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSearch = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var searchCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var definition = Substitute.For<IItemDefinition>();
        definition.Name.Returns("Test Item");
        var itemService = Substitute.For<IItemService>();
        itemService.GetTotalItemCount().Returns(_ =>
        {
            searchStarted.SetResult(true);
            releaseSearch.Task.GetAwaiter().GetResult();
            return 1;
        });
        itemService.FindItemDefinitionById(0).Returns(_ =>
        {
            searchCompleted.SetResult(true);
            return definition;
        });

        var area = Substitute.For<IArea>();
        area.IsPvP.Returns(false);
        var widgets = Substitute.For<IWidgetContainer>();
        var configurations = Substitute.For<IConfigurations>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IItemService)).Returns(itemService);
        var character = CreateCharacter();
        character.Area.Returns(area);
        character.Widgets.Returns(widgets);
        character.Configurations.Returns(configurations);
        character.ServiceProvider.Returns(serviceProvider);
        var destroyed = false;
        character.IsDestroyed.Returns(_ => destroyed);
        var command = new SpawnBoxCommand(Substitute.For<Hagalaz.Game.Abstractions.Builders.Item.IItemBuilder>());
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var task = new RsAsyncTask(() => command.Execute(new GameCommandArgs(character, ["test"])));

        scheduler.Schedule(task);
        scheduler.Tick();
        await searchStarted.Task;

        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!, default, default, default);
        widgets.DidNotReceiveWithAnyArgs().OpenWidget(default, default, default!, default);
        releaseSearch.SetResult(true);
        await searchCompleted.Task;
        destroyed = true;

        scheduler.Tick();
        scheduler.Tick();

        character.DidNotReceiveWithAnyArgs().SendChatMessage(default!, default, default, default);
        widgets.DidNotReceiveWithAnyArgs().OpenWidget(default, default, default!, default);
        configurations.DidNotReceiveWithAnyArgs().SendCs2Script(default, default!);
        configurations.DidNotReceiveWithAnyArgs().SendItems(default, default, default!);
    }

    private static ICharacter CreateCharacter()
    {
        var character = Substitute.For<ICharacter>();
        character.IsDestroyed.Returns(false);
        return character;
    }
}
