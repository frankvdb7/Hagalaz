using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Factories;

[TestClass]
public sealed class NpcChildScopeTests
{
    [TestMethod]
    public async Task RestoredFamiliar_ComposedByNpcBuilder_RetainsStateAfterNpcRegistration()
    {
        var definition = new NpcDefinition(6815)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test familiar",
            WalksRandomly = false,
        };
        var summoningDefinition = new SummoningDto { NpcId = definition.Id, Ticks = 100 };
        var restoredState = new HydratedFamiliar
        {
            TicksRemaining = 37,
            SpecialMovePoints = 12,
            IsUsingSpecialMove = true,
        };
        var restoredInventory = new[] { new HydratedItem(995, 3, 0, null) };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);
        npcService.RegisterAsync(Arg.Any<INpc>()).Returns(Task.CompletedTask);
        var eventManager = Substitute.For<IEventManager>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        mapRegionService.GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>())
            .Returns(Substitute.For<IMapRegion>());
        var summoner = Substitute.For<ICharacter>();
        summoner.EventManager.Returns(eventManager);
        var inventory = Substitute.For<IFamiliarInventoryContainer, IHydratable<IReadOnlyList<HydratedItem>>, IDehydratable<IReadOnlyList<HydratedItem>>>();
        var itemContainerFactory = Substitute.For<IItemContainerFactory>();
        itemContainerFactory.Create(summoner, StorageType.Normal, Arg.Any<int>()).Returns(inventory);

        var services = new ServiceCollection()
            .AddScoped<INpcScriptActivator, NpcScriptActivator>()
            .AddScoped<INpcService>(_ => npcService)
            .AddScoped<IItemContainerFactory>(_ => itemContainerFactory)
            .AddScoped<IItemService>(_ => Substitute.For<IItemService>())
            .AddScoped<IItemBuilder>(_ => Substitute.For<IItemBuilder>())
            .AddScoped<ISimplePathFinder>(_ => Substitute.For<ISimplePathFinder>())
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(eventManager)
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton(mapRegionService)
            .AddSingleton(Substitute.For<IAreaService>())
            .AddSingleton(Substitute.For<IProjectilePathFinder>())
            .AddSingleton<IOptions<CombatOptions>>(Options.Create(new CombatOptions()))
            .AddSingleton(Substitute.For<IHitSplatBuilder>())
            .AddSingleton(Substitute.For<ILootService>())
            .AddSingleton(Substitute.For<ILootGenerator>())
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .AddSingleton(Substitute.For<INpcScriptProvider>())
            .AddSingleton<IWidgetScriptActivator>(Substitute.For<IWidgetScriptActivator>())
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        var builder = new NpcBuilder(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<INpcScriptProvider>());
        var npc = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((activator, owner) =>
            {
                var script = (TestBobFamiliarScript)activator.Create(typeof(TestBobFamiliarScript), owner);
                script.Hydrate(restoredState);
                script.Hydrate(restoredInventory);
                script.AttachToSummoner(summoner, summoningDefinition);
                summoner.AttachFamiliar(script);
                return script;
            })
            .Spawn()
            .Npc;

        await npc.OnRegistered();

        var script = (TestBobFamiliarScript)npc.Script;
        Assert.AreEqual(37, script.Dehydrate().TicksRemaining);
        Assert.AreEqual(12, script.SpecialMovePoints);
        Assert.IsTrue(script.UsingSpecialMove);
        ((IHydratable<IReadOnlyList<HydratedItem>>)inventory).Received(1).Hydrate(restoredInventory);
    }

    [TestMethod]
    public void DynamicallySpawnedChildScript_UsesAndOwnsChildNpcScope()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);
        npcService.RegisterAsync(Arg.Any<INpc>()).Returns(Task.CompletedTask);

        var services = new ServiceCollection()
            .AddScoped<ScopeMarker>()
            .AddScoped<INpcScriptActivator, NpcScriptActivator>()
            .AddScoped<INpcService>(_ => npcService)
            .AddScoped<ISimplePathFinder>(_ => Substitute.For<ISimplePathFinder>())
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(Substitute.For<IEventManager>())
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton(Substitute.For<IMapRegionService>())
            .AddSingleton(Substitute.For<IProjectilePathFinder>())
            .AddSingleton<IOptions<CombatOptions>>(Options.Create(new CombatOptions()))
            .AddSingleton(Substitute.For<IHitSplatBuilder>())
            .AddSingleton(Substitute.For<ILootService>())
            .AddSingleton(Substitute.For<ILootGenerator>())
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .AddSingleton(Substitute.For<INpcScriptProvider>())
            .AddSingleton<IWidgetScriptActivator>(Substitute.For<IWidgetScriptActivator>())
            .BuildServiceProvider();

        using var parentScope = services.CreateScope();
        var builder = new NpcBuilder(parentScope.ServiceProvider, parentScope.ServiceProvider.GetRequiredService<INpcScriptProvider>());
        var parentMarker = parentScope.ServiceProvider.GetRequiredService<ScopeMarker>();
        var child = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((activator, owner) => activator.Create(typeof(ScopeAwareNpcScript), owner))
            .Spawn()
            .Npc;

        var script = (ScopeAwareNpcScript)child.Script;

        Assert.AreNotSame(parentMarker, script.Marker);
        Assert.IsFalse(script.Marker.Disposed);

        child.Destroy();

        Assert.IsTrue(script.Marker.Disposed);
    }

    public sealed class ScopeAwareNpcScript(
        INpc owner,
        INpcService npcService,
        ISimplePathFinder pathFinder,
        IWidgetScriptActivator widgetScriptActivator,
        ScopeMarker marker)
        : NpcScriptBase(owner, npcService, pathFinder, widgetScriptActivator)
    {
        public ScopeMarker Marker { get; } = marker;
    }

    public sealed class ScopeMarker : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    public sealed class TestBobFamiliarScript(
        INpc owner,
        IItemContainerFactory itemContainerFactory,
        ISmartPathFinder pathFinder,
        INpcService npcService,
        IItemService itemService,
        IGroundItemBuilder groundItemBuilder,
        IItemBuilder itemBuilder,
        IWidgetScriptActivator widgetScriptActivator)
        : BobFamiliarScriptBase(owner, itemContainerFactory, pathFinder, npcService, itemService, groundItemBuilder, itemBuilder, widgetScriptActivator)
    {
        public override int InventoryCapacity => 1;
    }
}
