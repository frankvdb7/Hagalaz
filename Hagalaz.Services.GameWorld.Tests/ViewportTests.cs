using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Collections;
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

            _viewport.RebuildView();
            _viewport.UpdateTick();

            // Act & Assert
            Assert.IsTrue(_viewport.VisibleCreatures.Contains(character));
        }

        [TestMethod]
        public void Indexer_ReturnsCorrectCreature_WhenMultipleCreaturesInViewport()
        {
            // Arrange
            var char1 = Substitute.For<ICharacter>();
            var char2 = Substitute.For<ICharacter>();
            var region = Substitute.For<IMapRegion>();

            var ownerLocation = Substitute.For<ILocation>();
            _owner.Location.Returns(ownerLocation);
            ownerLocation.WithinDistance(Arg.Any<ILocation>(), Arg.Any<int>()).Returns(true);
            ownerLocation.Clone().Returns(ownerLocation);

            char1.Location.Returns(Substitute.For<ILocation>());
            char1.Appearance.Visible.Returns(true);
            char2.Location.Returns(Substitute.For<ILocation>());
            char2.Appearance.Visible.Returns(true);

            region.FindAllCharacters().Returns(new List<ICharacter> { char1, char2 });
            region.FindAllNpcs().Returns(new List<INpc>());

            _regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IMapSize>())
                .Returns(new List<IMapRegion> { region });

            _viewport.RebuildView();
            _viewport.UpdateTick();

            // Act & Assert
            Assert.AreEqual(2, _viewport.VisibleCreatures.Count);
            Assert.AreEqual(char1, _viewport.VisibleCreatures[0]);
            Assert.AreEqual(char2, _viewport.VisibleCreatures[1]);
        }

        [TestMethod]
        public void VisibleCreatures_Empty_AfterClear()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var region = Substitute.For<IMapRegion>();

            var ownerLocation = Substitute.For<ILocation>();
            _owner.Location.Returns(ownerLocation);
            ownerLocation.WithinDistance(Arg.Any<ILocation>(), Arg.Any<int>()).Returns(true);
            ownerLocation.Clone().Returns(ownerLocation);

            character.Location.Returns(Substitute.For<ILocation>());
            character.Appearance.Visible.Returns(true);

            region.FindAllCharacters().Returns(new List<ICharacter> { character });
            region.FindAllNpcs().Returns(new List<INpc>());

            _regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IMapSize>())
                .Returns(new List<IMapRegion> { region });

            _viewport.RebuildView();
            _viewport.UpdateTick();
            Assert.AreEqual(1, _viewport.VisibleCreatures.Count);

            // Rebuild with no creatures
            region.FindAllCharacters().Returns(new List<ICharacter>());
            _viewport.UpdateTick();

            // Act & Assert
            Assert.AreEqual(0, _viewport.VisibleCreatures.Count);
            Assert.IsFalse(_viewport.VisibleCreatures.Contains(character));
        }
    }
}
