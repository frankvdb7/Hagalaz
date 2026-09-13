using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            regionService.FindMapRegion(location.RegionId, location.Dimension).Returns(region);

            var mapUpdateService = new MapUpdateService(regionLoadScheduler);

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
        regionService.FindMapRegion(location.RegionId, location.Dimension).Returns(region);

        viewport.RebuildView();
        var mapUpdateService = new MapUpdateService(regionLoadScheduler);

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
        regionService.FindMapRegion(location.RegionId, location.Dimension).Returns(region);

        new MapUpdateService(regionLoadScheduler).UpdateMap(character, false);

        regionLoadScheduler.Received(1).RequestLoad(region);
        region.DidNotReceive().SendFullPartUpdates(character);
        session.Received(1).SendMessage(Arg.Is<RaidoMessage>(message => message is DrawDynamicMapMessage));
    }

    [TestMethod]
    public void UpdateMap_RebindsStaleRegionAndOnlySchedulesTheReplacement()
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
        replacementRegion.State.Returns(MapRegionState.Initializing);
        replacementRegion.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
        regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
            .Returns(new[] { staleRegion });
        regionService.FindMapRegion(location.RegionId, location.Dimension).Returns(replacementRegion);

        new MapUpdateService(regionLoadScheduler).UpdateMap(character, false);

        regionLoadScheduler.Received(1).RequestLoad(replacementRegion);
        regionLoadScheduler.DidNotReceive().RequestLoad(staleRegion);
        staleRegion.DidNotReceive().SendFullPartUpdates(character);
        replacementRegion.DidNotReceive().SendFullPartUpdates(character);
    }
}
}
