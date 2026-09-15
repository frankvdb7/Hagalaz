using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class MapUpdateServiceTests
    {
        [TestMethod]
        public void UpdateMap_SendsMapAndRegionUpdatesSynchronously()
        {
            var character = Substitute.For<ICharacter>();
            var session = Substitute.For<IGameSession>();
            var location = new Location(100, 100, 0, 0);
            var region = Substitute.For<IMapRegion>();
            region.State.Returns(MapRegionState.Ready);
            var regionService = Substitute.For<IMapRegionService>();
            var regionLoadScheduler = Substitute.For<IMapRegionLoadScheduler>();
            var mapSize = Substitute.For<IMapSize>();
            var viewport = new Viewport(character, regionService, mapSize);

            character.Location.Returns(location);
            character.Index.Returns(1);
            character.Session.Returns(session);
            character.Viewport.Returns(viewport);
            mapSize.Size.Returns(104);
            mapSize.Type.Returns(0);
            region.Id.Returns(location.RegionId);
            region.BaseLocation.Returns(Location.Create(location.RegionX * 64, location.RegionY * 64, 0, 0));
            region.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
            regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
                .Returns(new[] { region });
            regionService.GetOrCreateMapRegion(location.RegionId, location.Dimension).Returns(region);

            var mapUpdateService = new MapUpdateService(regionLoadScheduler, regionService, Substitute.For<ICharacterStore>());

            mapUpdateService.UpdateMap(character, false, false);

            Received.InOrder(() =>
            {
                session.SendMessage(Arg.Any<RaidoMessage>());
                region.SendFullPartUpdates(character);
            });
            regionLoadScheduler.DidNotReceive().RequestLoad(region);
            session.Received(1).SendMessage(Arg.Is<RaidoMessage>(message => message is DrawStandardMapMessage));
        }

    [TestMethod]
    public void UpdateMap_ReusesViewportPreparedForWorldEntry()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var location = new Location(100, 100, 0, 0);
        var region = Substitute.For<IMapRegion>();
        region.State.Returns(MapRegionState.Ready);
        var regionService = Substitute.For<IMapRegionService>();
        var regionLoadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        var mapSize = Substitute.For<IMapSize>();
        var viewport = new Viewport(character, regionService, mapSize);

        character.Location.Returns(location);
        character.Index.Returns(1);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        mapSize.Size.Returns(104);
        mapSize.Type.Returns(0);
        region.Id.Returns(location.RegionId);
        region.BaseLocation.Returns(Location.Create(location.RegionX * 64, location.RegionY * 64, 0, 0));
        region.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
        regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
            .Returns(new[] { region });
        regionService.GetOrCreateMapRegion(location.RegionId, location.Dimension).Returns(region);

        viewport.RebuildView();
        var mapUpdateService = new MapUpdateService(regionLoadScheduler, regionService, Substitute.For<ICharacterStore>());

        mapUpdateService.UpdateMap(character, true, true);

        regionService.Received(1).GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize);
        regionLoadScheduler.DidNotReceive().RequestLoad(region);
        region.Received(1).SendFullPartUpdates(character);
    }

    [TestMethod]
    public void UpdateMap_RequestsInitializingRegionWithoutFullUpdates()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var location = new Location(100, 100, 0, 0);
        var region = Substitute.For<IMapRegion>();
        var regionService = Substitute.For<IMapRegionService>();
        var regionLoadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        var mapSize = Substitute.For<IMapSize>();
        var viewport = new Viewport(character, regionService, mapSize);

        character.Location.Returns(location);
        character.Index.Returns(1);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        mapSize.Size.Returns(104);
        mapSize.Type.Returns(0);
        region.Id.Returns(location.RegionId);
        region.BaseLocation.Returns(Location.Create(location.RegionX * 64, location.RegionY * 64, 0, 0));
        region.State.Returns(MapRegionState.Initializing);
        region.IsDynamic.Returns(true);
        region.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
        regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
            .Returns(new[] { region });
        regionService.GetOrCreateMapRegion(location.RegionId, location.Dimension).Returns(region);

        new MapUpdateService(regionLoadScheduler, regionService, Substitute.For<ICharacterStore>()).UpdateMap(character, false);

        regionLoadScheduler.Received(1).RequestLoad(region);
        region.DidNotReceive().SendFullPartUpdates(character);
        session.Received(1).SendMessage(Arg.Is<RaidoMessage>(message => message is DrawDynamicMapMessage));
    }

    [TestMethod]
    public void UpdateMap_RebindsStaleReadyRegionAndUpdatesTheReplacement()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var location = new Location(100, 100, 0, 0);
        var staleRegion = Substitute.For<IMapRegion>();
        var replacementRegion = Substitute.For<IMapRegion>();
        var regionService = Substitute.For<IMapRegionService>();
        var regionLoadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        var mapSize = Substitute.For<IMapSize>();
        var viewport = new Viewport(character, regionService, mapSize);

        character.Location.Returns(location);
        character.Index.Returns(1);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        mapSize.Size.Returns(104);
        mapSize.Type.Returns(0);
        staleRegion.Id.Returns(location.RegionId);
        staleRegion.BaseLocation.Returns(Location.Create(location.RegionX * 64, location.RegionY * 64, 0, 0));
        staleRegion.State.Returns(MapRegionState.Discarded);
        replacementRegion.Id.Returns(location.RegionId);
        replacementRegion.BaseLocation.Returns(staleRegion.BaseLocation);
        replacementRegion.State.Returns(MapRegionState.Ready);
        replacementRegion.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
        regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
            .Returns(new[] { staleRegion });
        regionService.GetOrCreateMapRegion(location.RegionId, location.Dimension).Returns(replacementRegion);

        new MapUpdateService(regionLoadScheduler, regionService, Substitute.For<ICharacterStore>()).UpdateMap(character, false);

        regionLoadScheduler.DidNotReceive().RequestLoad(Arg.Any<IMapRegion>());
        regionLoadScheduler.DidNotReceive().RequestLoad(staleRegion);
        staleRegion.DidNotReceive().SendFullPartUpdates(character);
        replacementRegion.Received(1).SendFullPartUpdates(character);
    }

    [TestMethod]
    public void UpdateMap_DeliversFullStateForLaterReadyVisibleRegion_OnceOnGameWorker()
    {
        var scenario = CreatePendingMapUpdateScenario();

        scenario.Service.UpdateMap(scenario.Character, false);
        scenario.Scheduler.Tick();
        scenario.Service.UpdateMap(scenario.Character, false);

        scenario.Character.Received(1).QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        scenario.PendingRegion.DidNotReceive().SendFullPartUpdates(scenario.Character);

        scenario.SetPendingState(MapRegionState.Ready);
        scenario.LoadGate.SetResult();
        scenario.Scheduler.Tick();

        scenario.PendingRegion.Received(1).SendFullPartUpdates(scenario.Character);
    }

    [TestMethod]
    public void UpdateMap_WhenVisibleInitializingSetChanges_DeliversOnlyTheCurrentRegion()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var characterStore = Substitute.For<ICharacterStore>();
        var regionService = Substitute.For<IMapRegionService>();
        var regionLoadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        var mapSize = Substitute.For<IMapSize>();
        var initialLocation = Location.Create(100, 100, 0, 0);
        var movedLocation = Location.Create(1000, 1000, 0, 0);
        var initialReadyRegion = CreateRegion(initialLocation, MapRegionState.Ready);
        var firstPendingRegion = CreateRegion(
            Location.Create(initialLocation.RegionX * 64 + 64, initialLocation.RegionY * 64, 0, 0),
            MapRegionState.Initializing);
        var movedReadyRegion = CreateRegion(movedLocation, MapRegionState.Ready);
        var secondPendingRegion = CreateRegion(
            Location.Create(movedLocation.RegionX * 64 + 64, movedLocation.RegionY * 64, 0, 0),
            MapRegionState.Initializing);
        var visibleRegions = new IMapRegion[] { initialReadyRegion, firstPendingRegion };
        var firstLoadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondLoadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var viewport = new Viewport(character, regionService, mapSize);

        character.Location.Returns(_ => visibleRegions[0] == initialReadyRegion ? initialLocation : movedLocation);
        character.Index.Returns(1);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        characterStore.FindByMasterId(42u).Returns(character);
        mapSize.Size.Returns(104);
        mapSize.Type.Returns(0);
        regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
            .Returns(_ => visibleRegions);
        regionService.GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>())
            .Returns(callInfo => ResolveRegion(
                callInfo[0] is int id ? id : throw new InvalidOperationException(),
                initialReadyRegion,
                firstPendingRegion,
                movedReadyRegion,
                secondPendingRegion));
        regionService.FindMapRegion(Arg.Any<int>(), Arg.Any<int>())
            .Returns(callInfo => ResolveRegion(
                callInfo[0] is int id ? id : throw new InvalidOperationException(),
                initialReadyRegion,
                firstPendingRegion,
                movedReadyRegion,
                secondPendingRegion));
        regionLoadScheduler.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var requested = callInfo.Arg<IEnumerable<IMapRegion>>()!.ToArray();
                return requested.Contains(firstPendingRegion)
                    ? firstLoadGate.Task
                    : secondLoadGate.Task;
            });
        character.When(value => value.QueueTask(Arg.Any<Func<CancellationToken, Task>>()))
            .Do(callInfo => scheduler.Schedule(new RsAsyncTask(callInfo.Arg<Func<CancellationToken, Task>>()!)));

        var service = new MapUpdateService(regionLoadScheduler, regionService, characterStore);

        service.UpdateMap(character, false);
        scheduler.Tick();

        visibleRegions = [movedReadyRegion, secondPendingRegion];
        service.UpdateMap(character, false);

        firstPendingRegion.State.Returns(MapRegionState.Ready);
        firstLoadGate.SetResult();
        scheduler.Tick();

        secondPendingRegion.State.Returns(MapRegionState.Ready);
        secondLoadGate.SetResult();
        scheduler.Tick();

        firstPendingRegion.DidNotReceive().SendFullPartUpdates(character);
        secondPendingRegion.Received(1).SendFullPartUpdates(character);
        regionLoadScheduler.Received(1).RequestLoad(firstPendingRegion);
        regionLoadScheduler.Received(1).RequestLoad(secondPendingRegion);
    }

    [TestMethod]
    public void UpdateMap_DoesNotSendLateStateAfterCharacterMoves()
    {
        var scenario = CreatePendingMapUpdateScenario();

        scenario.Service.UpdateMap(scenario.Character, false);
        scenario.Scheduler.Tick();
        scenario.SetLocation(Location.Create(1000, 1000, 0, 0));
        scenario.SetPendingState(MapRegionState.Ready);
        scenario.LoadGate.SetResult();
        scenario.Scheduler.Tick();

        scenario.PendingRegion.DidNotReceive().SendFullPartUpdates(scenario.Character);
    }

    [TestMethod]
    public void UpdateMap_DoesNotSendLateStateAfterCharacterLeavesStore()
    {
        var scenario = CreatePendingMapUpdateScenario();

        scenario.Service.UpdateMap(scenario.Character, false);
        scenario.Scheduler.Tick();
        scenario.CharacterStore.FindByMasterId(scenario.Character.MasterId).Returns((ICharacter?)null);
        scenario.SetPendingState(MapRegionState.Ready);
        scenario.LoadGate.SetResult();
        scenario.Scheduler.Tick();

        scenario.PendingRegion.DidNotReceive().SendFullPartUpdates(scenario.Character);
    }

    [TestMethod]
    public void UpdateMap_UsesCurrentCanonicalReplacementForLateState()
    {
        var scenario = CreatePendingMapUpdateScenario();
        var replacement = Substitute.For<IMapRegion>();
        var pendingRegionId = scenario.PendingRegion.Id;
        var pendingRegionBaseLocation = scenario.PendingRegion.BaseLocation;
        replacement.Id.Returns(pendingRegionId);
        replacement.BaseLocation.Returns(pendingRegionBaseLocation);
        replacement.State.Returns(MapRegionState.Ready);

        scenario.Service.UpdateMap(scenario.Character, false);
        scenario.Scheduler.Tick();
        scenario.SetCanonicalPendingRegion(replacement);
        scenario.RegionService.FindMapRegion(scenario.PendingRegion.Id, 0).Returns(replacement);
        scenario.SetPendingState(MapRegionState.Discarded);
        Assert.AreSame(replacement, scenario.RegionService.FindMapRegion(scenario.PendingRegion.Id, 0));
        Assert.IsFalse(scenario.Character.Viewport.ShouldRebuild());
        Assert.IsTrue(scenario.Character.Viewport.VisibleRegions.Any(region =>
            region.Id == replacement.Id
            && region.BaseLocation.Dimension == replacement.BaseLocation.Dimension));
        scenario.LoadGate.SetResult();
        scenario.Scheduler.Tick();

        scenario.PendingRegion.DidNotReceive().SendFullPartUpdates(scenario.Character);
        replacement.Received(1).SendFullPartUpdates(scenario.Character);
    }

    private static PendingMapUpdateScenario CreatePendingMapUpdateScenario()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var characterStore = Substitute.For<ICharacterStore>();
        var regionService = Substitute.For<IMapRegionService>();
        var regionLoadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        var mapSize = Substitute.For<IMapSize>();
        var location = Location.Create(100, 100, 0, 0);
        var regionA = Substitute.For<IMapRegion>();
        var pendingRegion = Substitute.For<IMapRegion>();
        var pendingState = MapRegionState.Initializing;
        var canonicalPendingRegion = pendingRegion;
        ILocation currentLocation = location;
        var loadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var viewport = new Viewport(character, regionService, mapSize);

        character.Location.Returns(_ => currentLocation);
        character.Index.Returns(1);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        characterStore.FindByMasterId(42u).Returns(character);
        mapSize.Size.Returns(104);
        mapSize.Type.Returns(0);

        regionA.Id.Returns(location.RegionId);
        regionA.BaseLocation.Returns(Location.Create(location.RegionX * 64, location.RegionY * 64, 0, 0));
        regionA.State.Returns(MapRegionState.Ready);
        regionA.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
        pendingRegion.Id.Returns(location.RegionId + 1);
        pendingRegion.BaseLocation.Returns(Location.Create((location.RegionX + 1) * 64, location.RegionY * 64, 0, 0));
        pendingRegion.State.Returns(_ => pendingState);
        pendingRegion.XteaKeys.Returns(new[] { 5, 6, 7, 8 });

        regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
            .Returns(new[] { regionA, pendingRegion });
        regionService.GetOrCreateMapRegion(regionA.Id, 0).Returns(regionA);
        regionService.GetOrCreateMapRegion(pendingRegion.Id, 0).Returns(pendingRegion);
        regionService.FindMapRegion(regionA.Id, 0).Returns(regionA);
        regionService.FindMapRegion(pendingRegion.Id, 0).Returns(_ => canonicalPendingRegion);
        regionLoadScheduler.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(loadGate.Task);
        character.When(value => value.QueueTask(Arg.Any<Func<CancellationToken, Task>>()))
            .Do(callInfo => scheduler.Schedule(new RsAsyncTask(callInfo.Arg<Func<CancellationToken, Task>>()!)));

        return new PendingMapUpdateScenario(
            new MapUpdateService(regionLoadScheduler, regionService, characterStore),
            character,
            characterStore,
            regionService,
            pendingRegion,
            loadGate,
            scheduler,
            value => pendingState = value,
            value => canonicalPendingRegion = value,
            value => currentLocation = value);
    }

    private static IMapRegion CreateRegion(ILocation location, MapRegionState state)
    {
        var region = Substitute.For<IMapRegion>();
        region.Id.Returns(location.RegionId);
        region.BaseLocation.Returns(Location.Create(location.RegionX * 64, location.RegionY * 64, 0, location.Dimension));
        region.State.Returns(state);
        region.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
        return region;
    }

    private static IMapRegion? ResolveRegion(
        int id,
        IMapRegion initialReadyRegion,
        IMapRegion firstPendingRegion,
        IMapRegion movedReadyRegion,
        IMapRegion secondPendingRegion) =>
        id switch
        {
            _ when id == initialReadyRegion.Id => initialReadyRegion,
            _ when id == firstPendingRegion.Id => firstPendingRegion,
            _ when id == movedReadyRegion.Id => movedReadyRegion,
            _ when id == secondPendingRegion.Id => secondPendingRegion,
            _ => null
        };

    private sealed record PendingMapUpdateScenario(
        MapUpdateService Service,
        ICharacter Character,
        ICharacterStore CharacterStore,
        IMapRegionService RegionService,
        IMapRegion PendingRegion,
        TaskCompletionSource LoadGate,
        RsTaskService Scheduler,
        Action<MapRegionState> SetPendingState,
        Action<IMapRegion> SetCanonicalPendingRegion,
        Action<ILocation> SetLocation);
}
}
