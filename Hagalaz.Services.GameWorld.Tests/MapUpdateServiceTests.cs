using System;
using System.Collections.Generic;
using System.Linq;
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

            new MapUpdateService(Substitute.For<IMapRegionService>()).UpdateMap(scenario.Character, false);

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

            new MapUpdateService(Substitute.For<IMapRegionService>()).UpdateMap(scenario.Character, false);

            Received.InOrder(() =>
            {
                scenario.Session.SendMessage(Arg.Is<RaidoMessage>(message => message is DrawDynamicMapMessage));
                scenario.Region.SendFullPartUpdates(scenario.Character);
            });
            scenario.Character.DidNotReceive().QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        }

        [TestMethod]
        public void UpdateMap_EnteringAReadyInstance_SendsTheEnterWorldMapBeforeAnyZoneStateInOneTick()
        {
            var scenario = CreateScenario(MapRegionState.Ready, true);

            new MapUpdateService(Substitute.For<IMapRegionService>()).UpdateMap(scenario.Character, true, true);
            scenario.Character.Viewport.UpdateTick();

            var message = scenario.Session.ReceivedCalls()
                .Select(call => call.GetArguments()[0])
                .OfType<DrawDynamicMapMessage>()
                .Single();
            Assert.IsTrue(message.RenderViewport);
            Assert.IsTrue(message.ForceUpdate);
            Received.InOrder(() =>
            {
                scenario.Session.SendMessage(Arg.Is<RaidoMessage>(sent => sent is DrawDynamicMapMessage));
                scenario.Region.SendFullPartUpdates(scenario.Character);
            });
            // the tick right after the draw does not synchronize the regions a second time
            scenario.Region.Received(1).SendFullPartUpdates(scenario.Character);
        }

        [TestMethod]
        public void UpdateMap_InitializingStandardRegion_SendsMapWithoutPrematureFullState()
        {
            var scenario = CreateScenario(MapRegionState.Initializing, false);

            new MapUpdateService(Substitute.For<IMapRegionService>()).UpdateMap(scenario.Character, false);

            scenario.Session.Received(1).SendMessage(Arg.Is<RaidoMessage>(message => message is DrawStandardMapMessage));
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

            new MapUpdateService(Substitute.For<IMapRegionService>()).UpdateMap(character, true, true);

            Received.InOrder(() =>
            {
                session.SendMessage(Arg.Is<RaidoMessage>(message => message is DrawStandardMapMessage));
                viewport.BeginMapRegionSynchronization();
            });
            character.DidNotReceive().QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        }

        [TestMethod]
        public void UpdateMap_DynamicViewport_SendsChunksInClientOrderWithFirstSeenSourceXteas()
        {
            var character = Substitute.For<ICharacter>();
            var session = Substitute.For<IGameSession>();
            var viewport = Substitute.For<IViewport>();
            var mapSize = Substitute.For<IMapSize>();
            var regionService = Substitute.For<IMapRegionService>();
            var location = Location.Create(3200, 3200, 0, 0);

            // 16 tiles -> 2x2 parts; the view origin part is 400 - 1 = 399 on both axes.
            mapSize.Size.Returns(16);
            mapSize.Type.Returns(0);
            var regions = new[] { CreateRegion(49, 49), CreateRegion(49, 50), CreateRegion(50, 49), CreateRegion(50, 50) };
            // slot (0,0) plane 0: plane 1, rotation 3, source region (112, 187)
            Present(regions[0], 7, 7, 0, 900, 1500, 1, 3);
            // slot (1,0) plane 0: descriptor value zero is still a present chunk, source region (0, 0)
            Present(regions[2], 0, 7, 0, 0, 0, 0, 0);
            // slot (1,1) plane 0: same source region as the first chunk, so no second XTEA group
            Present(regions[3], 0, 0, 0, 903, 1503, 0, 0);
            // slot (0,0) plane 1: new source region (1, 1)
            Present(regions[0], 7, 7, 1, 8, 8, 0, 0);
            regionService.GetXtea(112 << 8 | 187).Returns([1, 1, 1, 1]);
            regionService.GetXtea(0).Returns([2, 2, 2, 2]);
            regionService.GetXtea(1 << 8 | 1).Returns([3, 3, 3, 3]);

            character.Viewport.Returns(viewport);
            character.Session.Returns(session);
            character.Location.Returns(location);
            character.Index.Returns(1);
            viewport.VisibleRegions.Returns(regions);
            viewport.NeedsDynamicDraw().Returns(true);
            viewport.MapSize.Returns(mapSize);
            viewport.ViewLocation.Returns(location);

            new MapUpdateService(regionService).UpdateMap(character, true, true);

            var message = session.ReceivedCalls()
                .Select(call => call.GetArguments()[0])
                .OfType<DrawDynamicMapMessage>()
                .Single();
            Assert.IsTrue(message.ForceUpdate);
            Assert.IsTrue(message.RenderViewport);
            Assert.AreEqual(400, message.RegionPartX);
            Assert.AreEqual(400, message.RegionPartY);
            Assert.AreEqual(16, message.Chunks.Count);
            Assert.AreEqual(DynamicMapChunk.Present(900, 1500, 1, 3), message.Chunks[0]);
            Assert.AreEqual(DynamicMapChunk.Absent, message.Chunks[1]);
            Assert.AreEqual(DynamicMapChunk.Present(0, 0, 0, 0), message.Chunks[2]);
            Assert.AreEqual(DynamicMapChunk.Present(903, 1503, 0, 0), message.Chunks[3]);
            Assert.AreEqual(DynamicMapChunk.Present(8, 8, 0, 0), message.Chunks[4]);
            Assert.AreEqual(3, message.SourceRegionXteaKeys.Count);
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, message.SourceRegionXteaKeys[0]);
            CollectionAssert.AreEqual(new[] { 2, 2, 2, 2 }, message.SourceRegionXteaKeys[1]);
            CollectionAssert.AreEqual(new[] { 3, 3, 3, 3 }, message.SourceRegionXteaKeys[2]);
        }

        private static IMapRegion CreateRegion(int regionX, int regionY)
        {
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(regionX << 8 | regionY);
            return region;
        }

        private static void Present(IMapRegion region, int localPartX, int localPartY, int z, int sourcePartX, int sourcePartY, int sourceZ, int rotation)
        {
            var part = Substitute.For<IMapRegionPart>();
            part.HasDrawSource.Returns(true);
            part.DrawRegionPartX.Returns(sourcePartX);
            part.DrawRegionPartY.Returns(sourcePartY);
            part.DrawRegionZ.Returns(sourceZ);
            part.Rotation.Returns(rotation);
            region.GetRegionPartByLocalPart(localPartX, localPartY, z).Returns(part);
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
            // A 104-tile view around part (12, 12) spans regions 0..2 on both axes.
            var regions = new List<IMapRegion>();
            for (var regionX = 0; regionX <= 2; regionX++)
            {
                for (var regionY = 0; regionY <= 2; regionY++)
                {
                    var id = regionY + (regionX << 8);
                    var current = id == location.RegionId ? region : CreateRegion(regionX, regionY);
                    current.State.Returns(state);
                    current.IsDynamic.Returns(isDynamic);
                    regionService.GetOrCreateMapRegion(id, location.Dimension).Returns(current);
                    regions.Add(current);
                }
            }

            regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), mapSize).Returns(regions);

            return new Scenario(character, session, region, regions);
        }

        private sealed record Scenario(ICharacter Character, IGameSession Session, IMapRegion Region, IReadOnlyList<IMapRegion> Regions);
    }
}
