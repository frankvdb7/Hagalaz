using System.IO.Pipelines;
using System.Security.Claims;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureInteractionHubTests
{
    private readonly List<RaidoHubConnectionContext> _connections = [];
    private readonly List<(Pipe Input, Pipe Output)> _transports = [];

    [TestCleanup]
    public async Task CleanupConnections()
    {
        foreach (var connection in _connections)
        {
            connection.Abort();
            await connection.CleanupAsync();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }

    [TestMethod]
    public async Task OnNpcClick_WhenTargetRemainsOwnedAndVisible_InvokesCurrentNpc()
    {
        using var provider = CreateProvider(out _, out var npcService);
        var npcStore = provider.GetRequiredService<INpcStore>();
        var entityStore = provider.GetRequiredService<IEntityStore>();
        var npc = Substitute.For<INpc>();
        var script = Substitute.For<INpcScript>();
        npc.Script.Returns(script);
        Assert.IsTrue(npcStore.Add(npc));
        entityStore.Add(npc);
        npcService.FindByIndexAsync(npc.Index).Returns(new ValueTask<INpc?>(npc));

        var character = CreateCharacter(out var queuedTasks, new List<ICreature> { npc });
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(connection, new NpcClickMessage
        {
            Index = npc.Index,
            ClickType = NpcClickType.Option1Click,
            ForceRun = false
        });

        queuedTasks[0].Tick();

        script.Received(1).OnCharacterClick(character, NpcClickType.Option1Click, false);
    }

    [TestMethod]
    public async Task OnNpcClick_WhenTargetSlotIsReused_DropsTheOriginalClick()
    {
        using var provider = CreateProvider(out _, out var npcService);
        var npcStore = provider.GetRequiredService<INpcStore>();
        var entityStore = provider.GetRequiredService<IEntityStore>();
        var npc = Substitute.For<INpc>();
        var npcScript = Substitute.For<INpcScript>();
        npc.Script.Returns(npcScript);
        Assert.IsTrue(npcStore.Add(npc));
        entityStore.Add(npc);
        npcService.FindByIndexAsync(npc.Index).Returns(new ValueTask<INpc?>(npc));

        var character = CreateCharacter(out var queuedTasks, new List<ICreature> { npc });
        var connection = CreateConnection(character);
        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(connection, new NpcClickMessage
        {
            Index = npc.Index,
            ClickType = NpcClickType.Option1Click,
            ForceRun = false
        });

        Assert.IsTrue(npcStore.Remove(npc));
        Assert.IsTrue(entityStore.Remove(npc));
        var replacement = Substitute.For<INpc>();
        var replacementScript = Substitute.For<INpcScript>();
        replacement.Script.Returns(replacementScript);
        Assert.IsTrue(npcStore.Add(replacement));
        entityStore.Add(replacement);
        Assert.AreEqual(npc.Index, replacement.Index);

        queuedTasks[0].Tick();

        npcScript.DidNotReceive().OnCharacterClick(Arg.Any<ICharacter>(), Arg.Any<NpcClickType>(), Arg.Any<bool>());
        replacementScript.DidNotReceive().OnCharacterClick(Arg.Any<ICharacter>(), Arg.Any<NpcClickType>(), Arg.Any<bool>());
    }

    [TestMethod]
    public async Task OnComponentUseOnCharacter_WhenTargetRemainsOwned_InvokesCurrentTarget()
    {
        using var provider = CreateProvider(out var characterService, out _);
        var characterStore = provider.GetRequiredService<ICharacterStore>();
        var entityStore = provider.GetRequiredService<IEntityStore>();
        var target = Substitute.For<ICharacter>();
        target.MasterId.Returns(1u);
        Assert.IsTrue(await characterStore.AddAsync(target));
        entityStore.Add(target);
        characterService.FindByIndex(target.Index).Returns(new ValueTask<ICharacter?>(target));

        var widget = CreateOpenWidget(out var widgets);
        var character = CreateCharacter(out var queuedTasks, widgets: widgets);
        var connection = CreateConnection(character);
        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(connection, CreateCharacterUseMessage(target.Index));

        queuedTasks[0].Tick();

        widget.Received(1).OnComponentUsedOnCreature(7, target, false, 1, 2);
    }

    [TestMethod]
    public async Task OnComponentUseOnCharacter_WhenTargetSlotIsReused_DropsTheOriginalInteraction()
    {
        using var provider = CreateProvider(out var characterService, out _);
        var characterStore = provider.GetRequiredService<ICharacterStore>();
        var entityStore = provider.GetRequiredService<IEntityStore>();
        var target = Substitute.For<ICharacter>();
        target.MasterId.Returns(1u);
        Assert.IsTrue(await characterStore.AddAsync(target));
        entityStore.Add(target);
        characterService.FindByIndex(target.Index).Returns(new ValueTask<ICharacter?>(target));

        var widget = CreateOpenWidget(out var widgets);
        var character = CreateCharacter(out var queuedTasks, widgets: widgets);
        var connection = CreateConnection(character);
        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(connection, CreateCharacterUseMessage(target.Index));

        Assert.IsTrue(characterStore.Remove(target));
        Assert.IsTrue(entityStore.Remove(target));
        var replacement = Substitute.For<ICharacter>();
        replacement.MasterId.Returns(2u);
        Assert.IsTrue(await characterStore.AddAsync(replacement));
        entityStore.Add(replacement);
        Assert.AreEqual(target.Index, replacement.Index);

        queuedTasks[0].Tick();

        widget.DidNotReceive().OnComponentUsedOnCreature(
            Arg.Any<int>(), Arg.Any<ICreature>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [TestMethod]
    public async Task OnComponentUseOnNpc_WhenTargetSlotIsReused_DropsTheOriginalInteraction()
    {
        using var provider = CreateProvider(out _, out var npcService);
        var npcStore = provider.GetRequiredService<INpcStore>();
        var entityStore = provider.GetRequiredService<IEntityStore>();
        var target = Substitute.For<INpc>();
        Assert.IsTrue(npcStore.Add(target));
        entityStore.Add(target);
        npcService.FindByIndexAsync(target.Index).Returns(new ValueTask<INpc?>(target));

        var widget = CreateOpenWidget(out var widgets);
        var character = CreateCharacter(out var queuedTasks, widgets: widgets);
        var connection = CreateConnection(character);
        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(connection, CreateNpcUseMessage(target.Index));

        Assert.IsTrue(npcStore.Remove(target));
        Assert.IsTrue(entityStore.Remove(target));
        var replacement = Substitute.For<INpc>();
        Assert.IsTrue(npcStore.Add(replacement));
        entityStore.Add(replacement);
        Assert.AreEqual(target.Index, replacement.Index);

        queuedTasks[0].Tick();

        widget.DidNotReceive().OnComponentUsedOnCreature(
            Arg.Any<int>(), Arg.Any<ICreature>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>());
    }

    private static InterfaceComponentUseOnCharacterMessage CreateCharacterUseMessage(int index) => new()
    {
        InterfaceId = 10,
        ComponentId = 7,
        ExtraData1 = 1,
        ExtraData2 = 2,
        Index = index,
        ForceRun = false
    };

    private static InterfaceComponentUseOnNpcMessage CreateNpcUseMessage(int index) => new()
    {
        InterfaceId = 10,
        ComponentId = 7,
        ExtraData1 = 1,
        ExtraData2 = 2,
        Index = index,
        ForceRun = false
    };

    private static IWidget CreateOpenWidget(out IWidgetContainer widgets)
    {
        var widget = Substitute.For<IWidget>();
        widgets = Substitute.For<IWidgetContainer>();
        widgets.TryGetOpenWidget(10, out Arg.Any<IWidget>()).Returns(callInfo =>
        {
            callInfo[1] = widget;
            return true;
        });
        return widget;
    }

    private static ServiceProvider CreateProvider(
        out ICharacterService characterService,
        out INpcService npcService)
    {
        var characterServiceSubstitute = Substitute.For<ICharacterService>();
        var npcServiceSubstitute = Substitute.For<INpcService>();
        characterService = characterServiceSubstitute;
        npcService = npcServiceSubstitute;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICharacterService>(characterServiceSubstitute);
        services.AddSingleton<INpcService>(npcServiceSubstitute);
        services.AddSingleton<ICharacterStore>(CreateCharacterStore());
        services.AddSingleton<INpcStore, NpcStore>();
        var entityStore = new EntityStore();
        services.AddSingleton<IEntityStore>(entityStore);
        services.AddSingleton<IEntityService>(new EntityService(entityStore));
        services.AddRaidoServer().AddHub<NpcHub>().AddHub<ComponentHub>();
        return services.BuildServiceProvider();
    }

    private static CharacterStore CreateCharacterStore() => new(Options.Create(new GameServerOptions
    {
        ClientRevision = 1,
        ClientRevisionPatch = 0,
        AuthenticationToken = "test",
        Limits = { MaxConcurrentConnections = 10 }
    }));

    private RaidoHubConnectionContext CreateConnection(ICharacter character)
    {
        var features = new FeatureCollection();
        features.Set<IConnectionUserFeature>(new ConnectionUserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
        });
        var rawConnection = Substitute.For<ConnectionContext>();
        rawConnection.ConnectionId.Returns($"creature-hub-test-{_connections.Count}");
        rawConnection.Features.Returns(features);
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        rawConnection.Transport.Returns(transport);
        rawConnection.ConnectionClosed.Returns(CancellationToken.None);
        _transports.Add((input, output));

        var options = new RaidoConnectionContextOptions();
        var tcpConnection = new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance);
        Assert.IsTrue(tcpConnection.TryAttachPhysicalConnection(rawConnection));
        var connection = new RaidoHubConnectionContext(
            tcpConnection,
            options,
            Substitute.For<IRaidoProtocol>(),
            NullLoggerFactory.Instance,
            TimeProvider.System);
        connection.Features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        _connections.Add(connection);
        return connection;
    }

    private static ICharacter CreateCharacter(
        out List<ITaskItem> queuedTasks,
        IReadOnlyList<ICreature>? visibleCreatures = null,
        IWidgetContainer? widgets = null)
    {
        var tasks = new List<ITaskItem>();
        queuedTasks = tasks;
        var character = Substitute.For<ICharacter>();
        var viewport = Substitute.For<IViewport>();
        character.Viewport.Returns(viewport);
        viewport.VisibleCreatures.Returns(visibleCreatures ?? []);
        character.Widgets.Returns(widgets ?? Substitute.For<IWidgetContainer>());
        character.QueueTask(Arg.Any<ITaskItem>()).Returns(callInfo =>
        {
            tasks.Add(callInfo.Arg<ITaskItem>()!);
            return Substitute.For<IRsTaskHandle>();
        });
        return character;
    }

    private sealed class ConnectionUserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }
}
