using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
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
        public void UpdateMap_ReadyStandardRegion_SendsMapThenFullStateWithoutQueueing()
        {
            var scenario = CreateScenario(MapRegionState.Ready, false);

            new MapUpdateService().UpdateMap(scenario.Character, false);

            Received.InOrder(() =>
            {
                scenario.Session.SendMessage(Arg.Is<RaidoMessage>(message => message is DrawStandardMapMessage));
                scenario.Region.SendFullPartUpdates(scenario.Character);
            });
            scenario.Character.DidNotReceive().QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        }

        [TestMethod]
        public void UpdateMap_ReadyDynamicRegion_SendsDynamicMapThenFullStateWithoutQueueing()
        {
            var scenario = CreateScenario(MapRegionState.Ready, true);

            new MapUpdateService().UpdateMap(scenario.Character, false);

            Received.InOrder(() =>
            {
                scenario.Session.SendMessage(Arg.Is<RaidoMessage>(message => message is DrawDynamicMapMessage));
                scenario.Region.SendFullPartUpdates(scenario.Character);
            });
            scenario.Character.DidNotReceive().QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        }

        [TestMethod]
        public void UpdateMap_InitializingStandardRegion_SendsMapWithoutPrematureFullState()
        {
            var scenario = CreateScenario(MapRegionState.Initializing, false);

            new MapUpdateService().UpdateMap(scenario.Character, false);

            scenario.Session.Received(1).SendMessage(Arg.Is<RaidoMessage>(message => message is DrawStandardMapMessage));
            scenario.Region.DidNotReceive().SendFullPartUpdates(scenario.Character);
            scenario.Character.DidNotReceive().QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        }

        [TestMethod]
        public void UpdateMap_InitializingDynamicRegion_SendsDynamicMapWithoutPrematureFullState()
        {
            var scenario = CreateScenario(MapRegionState.Initializing, true);

            new MapUpdateService().UpdateMap(scenario.Character, false);

            scenario.Session.Received(1).SendMessage(Arg.Is<RaidoMessage>(message => message is DrawDynamicMapMessage));
            scenario.Region.DidNotReceive().SendFullPartUpdates(scenario.Character);
            scenario.Character.DidNotReceive().QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        }

        [TestMethod]
        public void UpdateMap_BeginsViewportSynchronizationAfterSendingMapPacket()
        {
            var character = Substitute.For<ICharacter>();
            var session = Substitute.For<IGameSession>();
            var viewport = Substitute.For<IViewport>();
            var mapSize = Substitute.For<IMapSize>();
            var location = Location.Create(100, 100, 0, 0);
            var region = Substitute.For<IMapRegion>();

            character.Viewport.Returns(viewport);
            character.Session.Returns(session);
            character.Location.Returns(location);
            viewport.VisibleRegions.Returns(new[] { region });
            viewport.ShouldRebuild().Returns(false);
            viewport.NeedsDynamicDraw().Returns(false);
            viewport.MapSize.Returns(mapSize);
            viewport.ViewLocation.Returns(location);
            mapSize.Type.Returns(0);

            new MapUpdateService().UpdateMap(character, true, true);

            Received.InOrder(() =>
            {
                session.SendMessage(Arg.Is<RaidoMessage>(message => message is DrawStandardMapMessage));
                viewport.BeginMapRegionSynchronization();
            });
            character.DidNotReceive().QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        }

        private static Scenario CreateScenario(MapRegionState state, bool isDynamic)
        {
            var character = Substitute.For<ICharacter>();
            var session = Substitute.For<IGameSession>();
            var regionService = Substitute.For<IMapRegionService>();
            var mapSize = Substitute.For<IMapSize>();
            var location = Location.Create(100, 100, 0, 0);
            var region = Substitute.For<IMapRegion>();
            var viewport = new Viewport(character, regionService, mapSize);

            character.Location.Returns(location);
            character.Index.Returns(1);
            character.Session.Returns(session);
            character.Viewport.Returns(viewport);
            mapSize.Size.Returns(104);
            mapSize.Type.Returns(0);
            region.Id.Returns(location.RegionId);
            region.BaseLocation.Returns(Location.Create(location.RegionX * 64, location.RegionY * 64, 0, 0));
            region.State.Returns(state);
            region.IsDynamic.Returns(isDynamic);
            region.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
            regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize)
                .Returns(new[] { region });
            regionService.GetOrCreateMapRegion(location.RegionId, location.Dimension).Returns(region);

            return new Scenario(character, session, region);
        }

        private sealed record Scenario(ICharacter Character, IGameSession Session, IMapRegion Region);
    }
}
