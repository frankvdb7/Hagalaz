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
            var regionService = Substitute.For<IMapRegionService>();
            var mapSize = Substitute.For<IMapSize>();
            var viewport = new Viewport(character, regionService, mapSize);

            character.Location.Returns(location);
            character.Index.Returns(1);
            character.Session.Returns(session);
            character.Viewport.Returns(viewport);
            mapSize.Size.Returns(104);
            mapSize.Type.Returns(0);
            region.XteaKeys.Returns(new[] { 1, 2, 3, 4 });
            regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), true, true, mapSize)
                .Returns(new[] { region });

            var mapUpdateService = new MapUpdateService(regionService);

            mapUpdateService.UpdateMap(character, false, false);

            Received.InOrder(() =>
            {
                session.SendMessage(Arg.Any<RaidoMessage>());
                regionService.EnsureRegionLoadScheduled(region);
                region.SendFullPartUpdates(character);
            });
            session.Received(1).SendMessage(Arg.Is<RaidoMessage>(message => message is DrawStandardMapMessage));
        }
    }
}
