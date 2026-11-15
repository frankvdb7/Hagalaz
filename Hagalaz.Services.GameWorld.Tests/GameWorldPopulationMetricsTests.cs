using System.Runtime.CompilerServices;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Metrics;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
[DoNotParallelize] // The gauges are process-wide, so other tests mutating stores would contaminate the deltas.
public sealed class GameWorldPopulationMetricsTests
{
    private const string MeterName = "Hagalaz.Services.GameWorld";
    private const string Characters = "hagalaz.gameworld.characters.registered";
    private const string Npcs = "hagalaz.gameworld.npcs.registered";
    private const string IdleRegions = "hagalaz.gameworld.regions.idle";
    private const string ReadyRegions = "hagalaz.gameworld.regions.ready";

    [TestMethod]
    public void Gauges_UseBoundedUnitsAndNoTags()
    {
        using var spy = CreateSpy();

        var values = spy.ObserveGauges();
        foreach (var name in new[] { Characters, Npcs, IdleRegions, ReadyRegions })
        {
            Assert.IsTrue(values.ContainsKey(name), name);
        }

        Assert.IsTrue(spy.Measurements.All(item => item.Tags.Count == 0));
        Assert.AreEqual("{character}", spy.Measurements.First(item => item.InstrumentName == Characters).Unit);
        Assert.AreEqual("{npc}", spy.Measurements.First(item => item.InstrumentName == Npcs).Unit);
        Assert.AreEqual("{region}", spy.Measurements.First(item => item.InstrumentName == IdleRegions).Unit);
        Assert.AreEqual("{region}", spy.Measurements.First(item => item.InstrumentName == ReadyRegions).Unit);
    }

    [TestMethod]
    public async Task CharacterStore_CountsOnlySuccessfulAddsAndRemoves()
    {
        using var spy = CreateSpy();
        var baseline = spy.ObserveGauges()[Characters];
        var store = new CharacterStore(Options.Create(new GameServerOptions
        {
            ClientRevision = 1,
            ClientRevisionPatch = 0,
            AuthenticationToken = "test"
        }));
        var first = Substitute.For<ICharacter>();
        var duplicate = Substitute.For<ICharacter>();
        var stranger = Substitute.For<ICharacter>();
        first.MasterId.Returns(42u);
        duplicate.MasterId.Returns(42u);
        stranger.MasterId.Returns(43u);

        Assert.IsTrue(await store.AddAsync(first));
        Assert.IsFalse(await store.AddAsync(duplicate));
        Assert.AreEqual(baseline + 1, spy.ObserveGauges()[Characters]);

        Assert.IsFalse(await store.RemoveAsync(stranger));
        Assert.IsTrue(await store.RemoveAsync(first));
        Assert.IsFalse(await store.RemoveAsync(first));
        Assert.IsFalse(store.Remove(first));
        Assert.AreEqual(baseline, spy.ObserveGauges()[Characters]);
    }

    [TestMethod]
    public async Task NpcStore_CountsOnlySuccessfulAddsAndRemoves()
    {
        using var spy = CreateSpy();
        var baseline = spy.ObserveGauges()[Npcs];
        var store = new NpcStore();
        var npc = Substitute.For<INpc>();

        Assert.IsTrue(await store.AddAsync(npc));
        Assert.IsTrue(store.Add(Substitute.For<INpc>()));
        Assert.AreEqual(baseline + 2, spy.ObserveGauges()[Npcs]);

        Assert.IsTrue(await store.RemoveAsync(npc));
        Assert.IsFalse(await store.RemoveAsync(npc));
        Assert.IsFalse(store.Remove(npc));
        Assert.AreEqual(baseline + 1, spy.ObserveGauges()[Npcs]);
    }

    [TestMethod]
    public void IdleRegions_TrackSuspendResumeAndRemoval()
    {
        using var spy = CreateSpy();
        var baseline = spy.ObserveGauges()[IdleRegions];
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
        region.MarkReady();
        var other = service.GetOrCreateMapRegion(2, 0);
        other.MarkReady();

        Assert.IsTrue(service.TrySuspendMapRegion(region));
        Assert.IsFalse(service.TrySuspendMapRegion(region));
        Assert.IsTrue(service.TrySuspendMapRegion(other));
        Assert.AreEqual(baseline + 2, spy.ObserveGauges()[IdleRegions]);

        Assert.AreSame(region, service.GetOrCreateMapRegion(1, 0));
        Assert.AreEqual(baseline + 1, spy.ObserveGauges()[IdleRegions]);

        Assert.IsFalse(service.TryRemoveIdleMapRegion(1, 0, region));
        Assert.IsTrue(service.TryRemoveIdleMapRegion(2, 0, other));
        Assert.IsFalse(service.TryRemoveIdleMapRegion(2, 0, other));
        Assert.AreEqual(baseline, spy.ObserveGauges()[IdleRegions]);
    }

    [TestMethod]
    public async Task Tick_RecordsReadyRegionsUsedByTheWorkerNotIdleOnes()
    {
        using var spy = CreateSpy();
        var ready = Substitute.For<IMapRegion>();
        ready.State.Returns(MapRegionState.Ready);
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindReadyRegions().Returns([ready, ready]);
        regionService.FindAllDimensions().Returns([]);
        regionService.FindIdleRegionsByDimension(Arg.Any<int>()).Returns([Substitute.For<IMapRegion>()]);
        var store = Substitute.For<ICharacterStore>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        store.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            new ValueTask<IReadOnlyDictionary<int, ICharacter>>(new Dictionary<int, ICharacter>()));
#pragma warning restore CA2012
        var worker = new GameWorkerService(
            Substitute.For<IRsTaskService>(),
            regionService,
            new MapRegionBackgroundService(regionService, Substitute.For<ILogger<MapRegionBackgroundService>>()),
            store,
            Options.Create(new GameServerOptions
            {
                AuthenticationToken = string.Empty,
                ClientRevision = 0,
                ClientRevisionPatch = 0,
                TickTimeSpan = TimeSpan.FromSeconds(1)
            }),
            Substitute.For<ILogger<GameWorkerService>>(),
            TimeProvider.System);

        await worker.ExecuteTickAsync(CancellationToken.None);

        var sample = spy.Measurements.Single(item => item.InstrumentName == "hagalaz.gameworld.tick.regions");
        Assert.AreEqual(2, sample.Value);
        Assert.AreEqual("{region}", sample.Unit);
        Assert.AreEqual(0, sample.Tags.Count);
        Assert.AreEqual(2, spy.ObserveGauges()[ReadyRegions]);
    }

    private static MeterListenerSpy CreateSpy()
    {
        // The instruments are created by the type initializer, which must run before the listener starts.
        RuntimeHelpers.RunClassConstructor(typeof(GameWorldMetrics).TypeHandle);
        return new MeterListenerSpy(MeterName);
    }

    private static ServiceProvider CreateProvider() => new ServiceCollection()
        .AddSingleton(Substitute.For<INpcService>())
        .AddSingleton<IEntityStore, EntityStore>()
        .AddSingleton(Substitute.For<IMapRegionLoadScheduler>())
        .AddSingleton<IRsTaskService>(_ => new RsTaskService(Substitute.For<ILogger<RsTaskService>>()))
        .BuildServiceProvider();

    private static MapRegionService CreateService(IServiceProvider provider) => new(
        provider,
        new LocationBuilder(),
        Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<ILogger<MapRegionService>>(),
        Substitute.For<IMapper>(),
        provider.GetRequiredService<IMapRegionLoadScheduler>(),
        provider.GetRequiredService<IRsTaskService>(),
        provider.GetRequiredService<IEntityStore>());
}
