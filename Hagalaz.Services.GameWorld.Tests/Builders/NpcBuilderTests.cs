using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Builders;
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
        npcService.RegisterAsync(Arg.Any<INpc>()).Returns(Task.CompletedTask);

        var script = Substitute.For<INpcScript>();
        var builder = CreateBuilder(npcService);

        var handle = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((_, _) => script)
            .Spawn();

        Assert.IsNotNull(handle.Npc);
        npcService.Received(1).RegisterAsync(handle.Npc);
    }

    [TestMethod]
    public void Spawn_WhenRegistrationFails_DestroysTheBuiltNpc()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);
        INpc? registeredNpc = null;
        npcService.RegisterAsync(Arg.Do<INpc>(npc => registeredNpc = npc))
            .Returns(Task.FromException(new InvalidOperationException("registration failed")));
        var builder = CreateBuilder(npcService);

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((_, _) => Substitute.For<INpcScript>())
            .Spawn());

        Assert.IsNotNull(registeredNpc);
        Assert.IsTrue(registeredNpc!.IsDestroyed);
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

    private static NpcBuilder CreateBuilder(INpcService npcService, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(Substitute.For<IEventManager>())
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton<IMapRegionService>(serviceProvider =>
            {
                var service = Substitute.For<IMapRegionService>();
                service.GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>())
                    .Returns(Substitute.For<IMapRegion>());
                return service;
            })
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

        public TScript CreateWithParent<TScript>(INpc owner, INpc parent) where TScript : INpcScript => throw new NotSupportedException();
    }

    private sealed class ScopeMarker : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

}
