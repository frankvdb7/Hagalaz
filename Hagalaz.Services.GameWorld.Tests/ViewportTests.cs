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
            Assert.IsInstanceOfType(_viewport.VisibleCreatures, typeof(ListHashSet<ICreature>));
            Assert.IsInstanceOfType(_viewport.VisibleCharacters, typeof(IReadOnlyCollection<ICharacter>));
            Assert.IsInstanceOfType(_viewport.VisibleNpcs, typeof(IReadOnlyCollection<INpc>));
        }

        [TestMethod]
        public void Contains_ReturnsTrue_WhenCreatureInViewport()
        {
            var character = Substitute.For<ICharacter>();
            character.Index.Returns(1);
            var region = Substitute.For<IMapRegion>();

            var ownerLoc = new Location(3200, 3200, 0, 0);
            _owner.Location.Returns(ownerLoc);

            var charLoc = new Location(3205, 3205, 0, 0);
            character.Location.Returns(charLoc);
            character.Appearance.Visible.Returns(true);

            var characters = new List<ICharacter> { character };
            region.CharacterCount.Returns(characters.Count);
            region.CopyCharactersTo(Arg.Any<ICharacter[]>(), Arg.Any<int>()).Returns(info => {
                var arr = info.Arg<ICharacter[]>();
                characters.CopyTo(arr, info.Arg<int>());
                return characters.Count;
            });
            region.NpcCount.Returns(0);

            _regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IMapSize>()).Returns(new List<IMapRegion> { region });

            _viewport.RebuildView();
            _viewport.UpdateTick();

            Assert.IsTrue(_viewport.VisibleCreatures.Contains(character));
        }

        [TestMethod]
        public void Indexer_ReturnsCorrectCreature_WhenMultipleCreaturesInViewport()
        {
            var char1 = Substitute.For<ICharacter>();
            char1.Index.Returns(1);
            var char2 = Substitute.For<ICharacter>();
            char2.Index.Returns(2);
            var region = Substitute.For<IMapRegion>();

            var ownerLoc = new Location(3200, 3200, 0, 0);
            _owner.Location.Returns(ownerLoc);

            char1.Location.Returns(new Location(3201, 3201, 0, 0));
            char1.Appearance.Visible.Returns(true);
            char2.Location.Returns(new Location(3202, 3202, 0, 0));
            char2.Appearance.Visible.Returns(true);

            var characters = new List<ICharacter> { char1, char2 };
            region.CharacterCount.Returns(characters.Count);
            region.CopyCharactersTo(Arg.Any<ICharacter[]>(), Arg.Any<int>()).Returns(info => {
                var arr = info.Arg<ICharacter[]>();
                characters.CopyTo(arr, info.Arg<int>());
                return characters.Count;
            });
            region.NpcCount.Returns(0);

            _regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IMapSize>()).Returns(new List<IMapRegion> { region });

            _viewport.RebuildView();
            _viewport.UpdateTick();

            Assert.AreEqual(2, _viewport.VisibleCreatures.Count);
            Assert.AreEqual(char1, _viewport.VisibleCreatures[0]);
            Assert.AreEqual(char2, _viewport.VisibleCreatures[1]);
        }

        [TestMethod]
        public void VisibleCreatures_Empty_AfterClear()
        {
            var character = Substitute.For<ICharacter>();
            character.Index.Returns(1);
            var region = Substitute.For<IMapRegion>();

            var ownerLoc = new Location(3200, 3200, 0, 0);
            _owner.Location.Returns(ownerLoc);

            character.Location.Returns(new Location(3201, 3201, 0, 0));
            character.Appearance.Visible.Returns(true);

            var characters = new List<ICharacter> { character };
            region.CharacterCount.Returns(characters.Count);
            region.CopyCharactersTo(Arg.Any<ICharacter[]>(), Arg.Any<int>()).Returns(info => {
                var arr = info.Arg<ICharacter[]>();
                characters.CopyTo(arr, info.Arg<int>());
                return characters.Count;
            });
            region.NpcCount.Returns(0);

            _regionService.GetMapRegionsWithinRange(Arg.Any<ILocation>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IMapSize>()).Returns(new List<IMapRegion> { region });

            _viewport.RebuildView();
            _viewport.UpdateTick();
            Assert.AreEqual(1, _viewport.VisibleCreatures.Count);

            region.CharacterCount.Returns(0);
            _viewport.UpdateTick();
            Assert.AreEqual(0, _viewport.VisibleCreatures.Count);
        }

        [TestMethod]
        public void InBounds_ReturnsTrue_WhenLocationIsWithinBounds()
        {
            _owner.Location.Returns(new Location(3200, 3200, 0, 0));
            _viewport.RebuildView();
            var location = new Location(3200, 3200, 0, 0);
            Assert.IsTrue(_viewport.InBounds(location));
        }

        [TestMethod]
        public void InBounds_ReturnsFalse_WhenLocationIsOutsideBounds()
        {
            _owner.Location.Returns(new Location(3200, 3200, 0, 0));
            _viewport.RebuildView();
            var location = new Location(3400, 3400, 0, 0);
            Assert.IsFalse(_viewport.InBounds(location));
        }

        [TestMethod]
        public void ShouldRebuild_ReturnsTrue_WhenDimensionChanges()
        {
            _owner.Location.Returns(new Location(3200, 3200, 0, 0));
            _viewport.RebuildView();
            _owner.Location.Returns(new Location(3200, 3200, 0, 1));
            Assert.IsTrue(_viewport.ShouldRebuild());
        }

        [TestMethod]
        public void ShouldRebuild_ReturnsFalse_WhenOwnerHasNotMovedMuch()
        {
            _owner.Location.Returns(new Location(3200, 3200, 0, 0));
            _viewport.RebuildView();
            _owner.Location.Returns(new Location(3204, 3204, 0, 0));
            Assert.IsFalse(_viewport.ShouldRebuild());
        }

        [TestMethod]
        public void InPreviousMapBounds_ReturnsCorrectValue_AfterRebuild()
        {
            _owner.Location.Returns(new Location(3200, 3200, 0, 0));
            _viewport.RebuildView();
            var oldLoc = new Location(3200, 3200, 0, 0);

            _owner.Location.Returns(new Location(4000, 4000, 0, 0));
            _viewport.RebuildView();

            Assert.IsTrue(_viewport.InPreviousMapBounds(oldLoc));
            Assert.IsFalse(_viewport.InPreviousMapBounds(new Location(4000, 4000, 0, 0)));
        }
    }
}
