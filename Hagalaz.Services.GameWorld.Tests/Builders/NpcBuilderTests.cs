using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Game.Common.Events;
using Hagalaz.Services.GameWorld.Data.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Builders;

[TestClass]
public sealed class NpcBuilderTests
{
    [TestMethod]
    public void Spawn_WithoutOptionalBoundsOrFaceDirection_BuildsAndRegistersNpc()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);

        var script = Substitute.For<INpcScript>();
        var scriptActivator = Substitute.For<INpcScriptActivator>();
        scriptActivator.Create(typeof(INpcScript), Arg.Any<INpc>()).Returns(script);
        var builder = CreateBuilder(npcService, services => services.AddSingleton(scriptActivator));

        var handle = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript(typeof(INpcScript))
            .Spawn();

        Assert.IsNotNull(handle.Npc);
        npcService.Received(1).Register(handle.Npc);
    }

    [TestMethod]
    public void Build_WhenScriptConstructionFails_DisposesNpcScope()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);
        var marker = new ScopeMarker();
        var builder = CreateBuilder(npcService, services =>
        {
            services.AddScoped(_ => marker);
            services.AddScoped<INpcScriptActivator, ThrowingNpcScriptActivator>();
        });

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .Build());

        Assert.IsTrue(marker.Disposed);
    }

    [TestMethod]
    public void Destroy_WhenScriptCleanupFails_RetriesScriptAndUnregistersIndependentHandlers()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);
        var script = Substitute.For<INpcScript>();
        var scriptCleanupAttempts = 0;
        var scriptFailure = new InvalidOperationException("script cleanup failed");
        script.When(value => value.OnDestroy()).Do(_ =>
        {
            if (++scriptCleanupAttempts == 1)
            {
                throw scriptFailure;
            }
        });
        var scriptActivator = Substitute.For<INpcScriptActivator>();
        scriptActivator.Create(typeof(INpcScript), Arg.Any<INpc>()).Returns(script);
        var eventManager = Substitute.For<IEventManager>();
        var region = Substitute.For<IMapRegion>();
        var regionService = Substitute.For<IMapRegionService>();
        regionService.GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>(), true).Returns(region);
        regionService.GetMapRegion(Arg.Any<int>(), Arg.Any<int>(), false, false).Returns(region);
        EventHappened eventHandle = _ => false;
        eventManager.Listen<CreatureDestroyedEvent>(Arg.Any<EventHappened<CreatureDestroyedEvent>>()).Returns(eventHandle);
        var builder = CreateBuilder(npcService, services =>
        {
            services.AddSingleton(scriptActivator);
            services.AddSingleton(eventManager);
            services.AddSingleton(regionService);
        });

        var npc = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript(typeof(INpcScript))
            .Build();
        npc.OnRegistered();
        npc.RegisterEventHandler<CreatureDestroyedEvent>(_ => false);

        var firstFailure = Assert.ThrowsExactly<InvalidOperationException>(() => npc.Destroy());

        Assert.AreSame(scriptFailure, firstFailure);
        Assert.IsTrue(npc.IsDestroyed);
        eventManager.Received(1).StopListen(typeof(CreatureDestroyedEvent), eventHandle);

        Assert.ThrowsExactly<InvalidOperationException>(() => npc.Destroy());

        Assert.IsTrue(npc.IsDestroyed);
        script.Received(1).OnDestroy();
        eventManager.Received(1).SendEvent(Arg.Is<IEvent>(value => value is CreatureDestroyedEvent));
    }

    private static NpcBuilder CreateBuilder(INpcService npcService, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(Substitute.For<IEventManager>())
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton(Substitute.For<IMapRegionService>())
            .AddSingleton(Substitute.For<IAreaService>())
            .AddSingleton(Substitute.For<IProjectilePathFinder>())
            .AddSingleton<IOptions<CombatOptions>>(Options.Create(new CombatOptions()))
            .AddSingleton(Substitute.For<IHitSplatBuilder>())
            .AddSingleton(npcService)
            .AddSingleton(Substitute.For<ILootService>())
            .AddSingleton(Substitute.For<ILootGenerator>())
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .AddSingleton(Substitute.For<INpcScriptProvider>())
            .AddSingleton(Substitute.For<INpcScriptActivator>());
        configure?.Invoke(services);
        var serviceProvider = services.BuildServiceProvider();
        return new NpcBuilder(serviceProvider, serviceProvider.GetRequiredService<INpcScriptProvider>());
    }

    private sealed class ThrowingNpcScriptActivator : INpcScriptActivator
    {
        public ThrowingNpcScriptActivator(ScopeMarker marker) => _ = marker;

        public INpcScript Create(Type scriptType, INpc owner) => throw new InvalidOperationException("script construction failed");

    }

    private sealed class ScopeMarker : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

}
