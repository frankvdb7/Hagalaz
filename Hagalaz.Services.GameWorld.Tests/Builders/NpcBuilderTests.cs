using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Cache.Abstractions.Types;
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
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Builders;

[TestClass]
public sealed class NpcBuilderTests
{
    [TestMethod]
    public void Spawn_WhenScriptActivationFails_DisposesNpcScope()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);
        var scriptActivator = Substitute.For<INpcScriptActivator>();
        scriptActivator.Create(Arg.Any<Type>(), Arg.Any<INpc>())
            .Returns(_ => throw new InvalidOperationException("script activation failed"));
        ScopedDependency? scopedDependency = null;

        var services = new ServiceCollection()
            .AddScoped<ScopedDependency>()
            .AddScoped<INpcScriptActivator>(serviceProvider =>
            {
                scopedDependency = serviceProvider.GetRequiredService<ScopedDependency>();
                return scriptActivator;
            })
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(Substitute.For<IEventManager>())
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton(Substitute.For<IMapRegionService>())
            .AddSingleton(Substitute.For<IProjectilePathFinder>())
            .AddSingleton<IOptions<CombatOptions>>(Options.Create(new CombatOptions()))
            .AddSingleton(Substitute.For<IHitSplatBuilder>())
            .AddSingleton(npcService)
            .AddSingleton(Substitute.For<ILootService>())
            .AddSingleton(Substitute.For<ILootGenerator>())
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .AddSingleton(Substitute.For<INpcScriptProvider>())
            .BuildServiceProvider();
        var builder = new NpcBuilder(services, services.GetRequiredService<INpcScriptProvider>());

        var scope = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((activator, owner) => activator.Create(typeof(NpcBuilderTests), owner));

        Assert.ThrowsExactly<InvalidOperationException>(() => scope.Spawn());

        Assert.IsNotNull(scopedDependency);
        Assert.IsTrue(scopedDependency!.IsDisposed);
    }

    [TestMethod]
    public void Spawn_WhenRegistrationFails_DestroysNpcAndDoesNotReturnHandle()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcService = Substitute.For<INpcService>();
        npcService.FindNpcDefinitionById(definition.Id).Returns(definition);
        npcService.RegisterAsync(Arg.Any<INpc>())
            .Returns(_ => throw new InvalidOperationException("registration failed"));

        var script = Substitute.For<INpcScript>();
        var mapRegion = Substitute.For<IMapRegion>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        mapRegionService.GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>()).Returns(mapRegion);
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(Substitute.For<IEventManager>())
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton(mapRegionService)
            .AddSingleton(Substitute.For<IProjectilePathFinder>())
            .AddSingleton<IOptions<CombatOptions>>(Options.Create(new CombatOptions()))
            .AddSingleton(Substitute.For<IHitSplatBuilder>())
            .AddSingleton(npcService)
            .AddSingleton(Substitute.For<ILootService>())
            .AddSingleton(Substitute.For<ILootGenerator>())
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .AddSingleton(Substitute.For<INpcScriptProvider>())
            .AddSingleton(Substitute.For<INpcScriptActivator>())
            .BuildServiceProvider();
        var builder = new NpcBuilder(services, services.GetRequiredService<INpcScriptProvider>());

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((_, _) => script)
            .Spawn());

        script.Received(1).OnDestroy();
    }

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
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(Substitute.For<IEventManager>())
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton(Substitute.For<IMapRegionService>())
            .AddSingleton(Substitute.For<IProjectilePathFinder>())
            .AddSingleton<IOptions<CombatOptions>>(Options.Create(new CombatOptions()))
            .AddSingleton(Substitute.For<IHitSplatBuilder>())
            .AddSingleton(npcService)
            .AddSingleton(Substitute.For<ILootService>())
            .AddSingleton(Substitute.For<ILootGenerator>())
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .AddSingleton(Substitute.For<INpcScriptProvider>())
            .AddSingleton(Substitute.For<INpcScriptActivator>())
            .BuildServiceProvider();
        var builder = new NpcBuilder(services, services.GetRequiredService<INpcScriptProvider>());

        var handle = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((_, _) => script)
            .Spawn();

        Assert.IsNotNull(handle.Npc);
        npcService.Received(1).RegisterAsync(handle.Npc);

        handle.Npc.Script.OnSpawn();
        handle.Npc.Script.Received(1).OnSpawn();
    }

    [TestMethod]
    public async Task BuildThenRegister_WhenRegistrationFails_DestroysNpcAndDisposesNpcScope()
    {
        var definition = new NpcDefinition(1)
        {
            BoundsType = BoundsType.Static,
            DisplayName = "Test NPC",
            WalksRandomly = false,
        };
        var npcTypeProvider = Substitute.For<ITypeProvider<INpcType>>();
        npcTypeProvider.Get(definition.Id).Returns(definition);
        var mapper = Substitute.For<IMapper>();
        mapper.Map<INpcDefinition>(definition).Returns(definition);
        var npcDefinitionStore = new NpcDefinitionStore(
            Substitute.For<IServiceProvider>(),
            npcTypeProvider,
            mapper,
            Substitute.For<ILogger<NpcDefinitionStore>>());
        var npcStore = new TrackingNpcStore();
        var npcService = new NpcService(
            npcStore,
            npcDefinitionStore,
            Substitute.For<ITypeProvider<INpcDefinition>>(),
            Substitute.For<ILogger<NpcService>>());
        var script = Substitute.For<INpcScript>();
        var expectedException = new InvalidOperationException("registration failed");
        script.When(x => x.OnSpawn()).Do(_ => throw expectedException);
        var mapRegion = Substitute.For<IMapRegion>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        var area = Substitute.For<IArea>();
        var areaService = Substitute.For<IAreaService>();
        areaService.FindAreaByLocation(Arg.Any<ILocation>()).Returns(area);
        mapRegionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IMapSize>())
            .Returns(new[] { mapRegion });
        mapRegionService.GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>()).Returns(mapRegion);
        ScopedDependency? scopedDependency = null;

        var services = new ServiceCollection()
            .AddScoped<ScopedDependency>()
            .AddSingleton<INpcService>(npcService)
            .AddSingleton(Substitute.For<ICreatureTaskService>())
            .AddSingleton(Substitute.For<IEventManager>())
            .AddSingleton(Substitute.For<IScopedGameMediator>())
            .AddSingleton(Substitute.For<ISmartPathFinder>())
            .AddSingleton(mapRegionService)
            .AddSingleton(areaService)
            .AddSingleton(Substitute.For<IProjectilePathFinder>())
            .AddSingleton<IOptions<CombatOptions>>(Options.Create(new CombatOptions()))
            .AddSingleton(Substitute.For<IHitSplatBuilder>())
            .AddSingleton(Substitute.For<ILootService>())
            .AddSingleton(Substitute.For<ILootGenerator>())
            .AddSingleton(Substitute.For<IGroundItemBuilder>())
            .AddSingleton(Substitute.For<INpcScriptProvider>())
            .AddSingleton(Substitute.For<INpcScriptActivator>())
            .BuildServiceProvider();
        var builder = new NpcBuilder(services, services.GetRequiredService<INpcScriptProvider>());

        var npc = builder.Create()
            .WithId(definition.Id)
            .WithLocation(new Location(3200, 3200, 0, 0))
            .WithScript((_, owner) =>
            {
                scopedDependency = owner.ServiceProvider.GetRequiredService<ScopedDependency>();
                return script;
            })
            .Build();

        var actualException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => npcService.RegisterAsync(npc));

        Assert.AreSame(expectedException, actualException);
        Assert.IsTrue(npc.IsDestroyed);
        Assert.AreSame(npc, npcStore.RemovedNpc);
        Assert.IsNotNull(scopedDependency);
        Assert.IsTrue(scopedDependency!.IsDisposed);
        mapRegion.Received(1).Remove(npc);
    }

    private sealed class ScopedDependency : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class TrackingNpcStore : INpcStore
    {
        public INpc? RemovedNpc { get; private set; }

        public IAsyncEnumerable<INpc> FindAllAsync() => throw new NotSupportedException();

        public ValueTask<int> CountAsync() => throw new NotSupportedException();

        public ValueTask<bool> AddAsync(INpc npc) => new(true);

        public ValueTask<bool> RemoveAsync(INpc npc)
        {
            RemovedNpc = npc;
            return new(true);
        }

        public ValueTask<INpc?> FindAsync(Func<INpc, bool> predicate) => throw new NotSupportedException();
    }
}
