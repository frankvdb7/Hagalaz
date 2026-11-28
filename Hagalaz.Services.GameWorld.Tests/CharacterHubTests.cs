using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Server;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterHubTests
{
    private readonly HubConnectionHarness _hubConnections = new();

    [TestCleanup]
    public async Task CleanupConnections()
    {
        await _hubConnections.CleanupAsync();
    }

    [TestMethod]
    public async Task OnMovement_QueuesGameplayUntilTheGameTaskRuns()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        var connection = CreateConnection(character);
        var message = new MovementMessage { AbsX = 3201, AbsY = 3202, ForceRun = true };

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(connection, message);

        Assert.HasCount(1, queuedTasks);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        character.DidNotReceive().Interrupt(Arg.Any<object>());

        character.Location.Returns(Location.Create(3200, 3200, 3, 9));
        queuedTasks[0].Tick();

        var walkEvent = eventManager.ReceivedCalls()
            .Select(call => call.GetArguments().Single())
            .OfType<WalkAllowEvent>()
            .Single();
        Assert.AreEqual(3201, walkEvent.TargetLocation.X);
        Assert.AreEqual(3202, walkEvent.TargetLocation.Y);
        Assert.AreEqual(3, walkEvent.TargetLocation.Z);
        Assert.AreEqual(9, walkEvent.TargetLocation.Dimension);
        character.Received(1).Interrupt(Arg.Any<object>());
        Assert.AreEqual(MovementType.Run, character.Movement.MovementType);
        Assert.HasCount(2, queuedTasks);
    }

    [TestMethod]
    public async Task OnMovement_RejectedByWalkAllowDoesNotInterruptOrQueueReachTask()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        eventManager.SendEvent(Arg.Any<IEvent>()).Returns(call => call.Arg<IEvent>() is not WalkAllowEvent);
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new MovementMessage { AbsX = 3201, AbsY = 3202, ForceRun = true });

        queuedTasks[0].Tick();

        character.DidNotReceive().Interrupt(Arg.Any<object>());
        Assert.HasCount(1, queuedTasks);
    }

    [TestMethod]
    public async Task OnPublicChat_QueuesChatDecisionUntilTheGameTaskRuns()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new PublicChatMessage { Text = "hello", TextAnimation = 0, TextColor = 0 });

        Assert.HasCount(1, queuedTasks);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());

        queuedTasks[0].Tick();

        eventManager.Received(1).SendEvent(Arg.Is<ChatAllowEvent>(value => value.Text == "hello"));
    }

    [TestMethod]
    public async Task OnMusicPlayed_QueuesMusicNotificationUntilTheGameTaskRuns()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out _, out var queuedTasks);
        var music = Substitute.For<IMusic>();
        character.Music.Returns(music);
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new MusicPlayedMessage { MusicId = 42 });

        music.DidNotReceive().OnMusicPlayed(Arg.Any<int>());

        queuedTasks[0].Tick();

        music.Received(1).OnMusicPlayed(42);
    }

    [TestMethod]
    public async Task PublicChatThenSetClientChatType_PreservesFifoOrdering()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        var chat = TrackChatTypes(character, eventManager);
        var observedTypes = chat.Observed;
        var connection = CreateConnection(character);

        var dispatcher = provider.GetRequiredService<IRaidoDispatcher>();
        await dispatcher.DispatchMessageAsync(connection, new PublicChatMessage
        {
            Text = "hello",
            TextAnimation = 0,
            TextColor = 0
        });
        await dispatcher.DispatchMessageAsync(connection, new SetClientChatTypeMessage { Type = ClientChatType.Friends });

        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        foreach (var task in queuedTasks)
        {
            scheduler.Schedule(task);
        }

        scheduler.Tick();

        Assert.AreEqual(ClientChatType.Normal, observedTypes.Single());
        Assert.AreEqual(ClientChatType.Friends, chat.Current);
    }

    [TestMethod]
    public async Task SetClientChatTypeThenPublicChat_PreservesFifoOrdering()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        var chat = TrackChatTypes(character, eventManager);
        var observedTypes = chat.Observed;
        var connection = CreateConnection(character);

        var dispatcher = provider.GetRequiredService<IRaidoDispatcher>();
        await dispatcher.DispatchMessageAsync(connection, new SetClientChatTypeMessage { Type = ClientChatType.Friends });
        await dispatcher.DispatchMessageAsync(connection, new PublicChatMessage
        {
            Text = "hello",
            TextAnimation = 0,
            TextColor = 0
        });

        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        foreach (var task in queuedTasks)
        {
            scheduler.Schedule(task);
        }

        scheduler.Tick();

        Assert.AreEqual(ClientChatType.Friends, observedTypes.Single());
        Assert.AreEqual(ClientChatType.Friends, chat.Current);
    }

    [TestMethod]
    public async Task OnCharacterClick_WhenTargetRemainsOwnedAndVisible_InvokesClickOnCurrentTarget()
    {
        using var provider = CreateProvider();
        var owned = await AddOwnedTargetAsync(provider);
        var target = owned.Target;

        var character = CreateCharacter(out _, out var queuedTasks);
        character.Viewport.VisibleCreatures.Returns(new List<ICreature> { target });
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new CharacterClickMessage { Index = target.Index, ClickType = CharacterClickType.Option1Click, ForceRun = false });

        queuedTasks[0].Tick();

        character.Received(1).OnCharacterClicked(CharacterClickType.Option1Click, false, target);
    }

    [TestMethod]
    public async Task OnCharacterClick_WhenTargetIsRemovedBeforeQueuedTask_DropsClick()
    {
        using var provider = CreateProvider();
        var owned = await AddOwnedTargetAsync(provider);
        var target = owned.Target;
        var characterStore = owned.Store;
        var entityStore = owned.Entities;

        var (character, queuedTasks) = await ClickTargetThenRemoveItAsync(provider, owned);
        queuedTasks[0].Tick();

        character.DidNotReceive().OnCharacterClicked(
            Arg.Any<CharacterClickType>(), Arg.Any<bool>(), Arg.Any<ICharacter>());
    }

    [TestMethod]
    public async Task OnCharacterClick_WhenTargetSlotIsReused_DoesNotRedirectClickToReplacement()
    {
        using var provider = CreateProvider();
        var owned = await AddOwnedTargetAsync(provider);
        var target = owned.Target;
        var characterStore = owned.Store;
        var entityStore = owned.Entities;

        var (character, queuedTasks) = await ClickTargetThenRemoveItAsync(provider, owned);
        var replacement = EntityTestFactory.Create<ICharacter>();
        replacement.MasterId.Returns(2u);
        Assert.IsTrue(await characterStore.AddAsync(replacement));
        entityStore.Add(replacement);
        Assert.AreEqual(target.Index, replacement.Index);

        queuedTasks[0].Tick();

        character.DidNotReceive().OnCharacterClicked(
            Arg.Any<CharacterClickType>(), Arg.Any<bool>(), Arg.Any<ICharacter>());
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICharacterService>());
        services.AddSingleton(Substitute.For<IAuthenticationService>());
        services.AddSingleton<ICharacterStore>(CreateCharacterStore());
        var entityStore = new EntityStore();
        services.AddSingleton<IEntityStore>(entityStore);
        services.AddSingleton<IEntityService>(new EntityService(entityStore));
        services.AddRaidoServer().AddHub<CharacterHub>();
        return services.BuildServiceProvider();
    }

    private static CharacterStore CreateCharacterStore() => new(Options.Create(new GameServerOptions
    {
        ClientRevision = 1,
        ClientRevisionPatch = 0,
        AuthenticationToken = "test",
        Limits = { MaxConcurrentConnections = 10 }
    }));

    private RaidoHubConnectionContext CreateConnection(ICharacter character) => _hubConnections.CreateConnection(character, "character-hub-test");


    /// <summary>
    ///     Records the character's chat type each time a chat is checked, and makes the chat type a settable property.
    /// </summary>
    private static ChatTypeTracker TrackChatTypes(ICharacter character, IEventManager eventManager)
    {
        var tracker = new ChatTypeTracker();
        character.CurrentChatType.Returns(_ => tracker.Current);
        character.WhenForAnyArgs(value => value.CurrentChatType = default)
            .Do(callInfo => tracker.Current = callInfo.Arg<ClientChatType>());
        var observedTypes = tracker.Observed;
        eventManager.SendEvent(Arg.Any<IEvent>()).Returns(callInfo =>
        {
            if (callInfo.Arg<IEvent>() is ChatAllowEvent)
            {
                observedTypes.Add(character.CurrentChatType);
            }

            return true;
        });
        return tracker;
    }

    private sealed class ChatTypeTracker
    {
        public ClientChatType Current { get; set; } = ClientChatType.Normal;
        public List<ClientChatType> Observed { get; } = [];
    }

    /// <summary>
    ///     A character owned by master 1, registered in the character and entity stores and findable by index.
    /// </summary>

    /// <summary>
    ///     A character that is viewing the target clicks it, and the target is then removed from both stores before
    ///     the queued click task runs.
    /// </summary>
    private async Task<(ICharacter Character, List<ITaskItem> QueuedTasks)> ClickTargetThenRemoveItAsync(ServiceProvider provider, OwnedTarget owned)
    {
        var character = CreateCharacter(out _, out var queuedTasks);
        character.Viewport.VisibleCreatures.Returns(new List<ICreature> { owned.Target });
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new CharacterClickMessage { Index = owned.Target.Index, ClickType = CharacterClickType.Option1Click, ForceRun = false });

        Assert.IsTrue(owned.Store.Remove(owned.Target));
        Assert.IsTrue(owned.Entities.Remove(owned.Target));
        return (character, queuedTasks);
    }

    private static async Task<OwnedTarget> AddOwnedTargetAsync(ServiceProvider provider)
    {
        var characterService = provider.GetRequiredService<ICharacterService>();
        var characterStore = provider.GetRequiredService<ICharacterStore>();
        var entityStore = provider.GetRequiredService<IEntityStore>();
        var target = EntityTestFactory.Create<ICharacter>();
        target.MasterId.Returns(1u);
        Assert.IsTrue(await characterStore.AddAsync(target));
        entityStore.Add(target);
        characterService.FindByIndex(target.Index)
            .Returns(new ValueTask<ICharacter?>(target));
        return new OwnedTarget(characterService, characterStore, entityStore, target);
    }

    private sealed record OwnedTarget(ICharacterService Service, ICharacterStore Store, IEntityStore Entities, ICharacter Target);

    private static ICharacter CreateCharacter(out IEventManager eventManager, out List<ITaskItem> queuedTasks)
    {
        eventManager = Substitute.For<IEventManager>();
        eventManager.SendEvent(Arg.Any<IEvent>()).Returns(true);
        var tasks = new List<ITaskItem>();
        queuedTasks = tasks;
        var character = EntityTestFactory.Create<ICharacter>();
        var location = Location.Create(3200, 3200, 1, 7);
        var movement = Substitute.For<IMovement>();
        var viewport = Substitute.For<IViewport>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var pathFinderProvider = Substitute.For<IPathFinderProvider>();
        pathFinderProvider.Smart.Returns(Substitute.For<ISmartPathFinder>());

        character.Location.Returns(location);
        character.Movement.Returns(movement);
        character.Viewport.Returns(viewport);
        character.ServiceProvider.Returns(serviceProvider);
        character.EventManager.Returns(eventManager);
        viewport.VisibleCharacters.Returns(Array.Empty<ICharacter>());
        serviceProvider.GetService(typeof(IPathFinderProvider)).Returns(pathFinderProvider);
        character.QueueTask(Arg.Do<ITaskItem>(task => tasks.Add(task)))
            .Returns(Substitute.For<IRsTaskHandle>());
        return character;
    }
}
