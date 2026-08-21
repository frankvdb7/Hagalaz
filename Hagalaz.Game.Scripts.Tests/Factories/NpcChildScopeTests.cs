using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
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
    public void DynamicallySpawnedChildScript_UsesChildNpcScope()
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
        var parentNpc = Substitute.For<INpc>();

        var child = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((activator, owner) => activator.Create(typeof(ScopeAwareNpcScript), owner, parentNpc))
            .Spawn()
            .Npc;

        var script = (ScopeAwareNpcScript)child.Script;

        Assert.AreNotSame(parentMarker, script.Marker);
        Assert.AreSame(parentNpc, script.Parent);
    }

    public sealed class ScopeAwareNpcScript(
        INpc owner,
        INpc parent,
        INpcService npcService,
        ISimplePathFinder pathFinder,
        IWidgetScriptActivator widgetScriptActivator,
        ScopeMarker marker)
        : NpcScriptBase(owner, npcService, pathFinder, widgetScriptActivator)
    {
        public ScopeMarker Marker { get; } = marker;

        public INpc Parent { get; } = parent;
    }

    public sealed class ScopeMarker
    {
    }
}
