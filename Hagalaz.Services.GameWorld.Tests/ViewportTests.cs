using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class ViewportTests
    {
        private ICreature _owner = null!;
        private IMapRegionService _regionService = null!;
        private IMapSize _mapSize = null!;
        private Viewport _viewport = null!;

        [TestInitialize]
        public void Setup()
        {
            _owner = Substitute.For<ICreature>();
            _regionService = Substitute.For<IMapRegionService>();
            _mapSize = Substitute.For<IMapSize>();
            _mapSize.Size.Returns(104);
            _viewport = new Viewport(_owner, _regionService, _mapSize);
        }

        [TestMethod]
        public void VisibleCreatures_IsListHashSet_Internally()
        {
            // Assert
            Assert.IsInstanceOfType(_viewport.VisibleCreatures, typeof(ListHashSet<ICreature>));
        }

        [TestMethod]
        public void Contains_ReturnsTrue_WhenCreatureInViewport()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var region = Substitute.For<IMapRegion>();

            // Set up owner location
            var ownerLocation = Substitute.For<ILocation>();
            _owner.Location.Returns(ownerLocation);
            ownerLocation.WithinDistance(Arg.Any<ILocation>(), Arg.Any<int>()).Returns(true);
            ownerLocation.Clone().Returns(ownerLocation);

            // Set up character
            var characterLocation = Substitute.For<ILocation>();
            character.Location.Returns(characterLocation);
            character.Appearance.Visible.Returns(true);

            region.FindAllCharacters().Returns(new List<ICharacter> { character });
            region.FindAllNpcs().Returns(new List<INpc>());

            _regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IMapSize>())
                .Returns(new List<IMapRegion> { region });

            // Mock InBounds to return true
            // Viewport.InBounds uses BoundsMinimum/Maximum which are set in RebuildView
            // We need to make sure those locations work.

            _viewport.RebuildView();
            _viewport.UpdateTick();

            // Act & Assert
            Assert.IsTrue(_viewport.VisibleCreatures.Contains(character));
        }
    }
}
