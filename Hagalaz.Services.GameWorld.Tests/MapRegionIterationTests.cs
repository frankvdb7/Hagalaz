using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class MapRegionIterationTests
    {
        private ILocation _baseLocation = null!;
        private INpcService _npcService = null!;
        private IMapRegionService _regionService = null!;
        private IGameObjectBuilder _gameObjectBuilder = null!;
        private IGroundItemBuilder _groundItemBuilder = null!;
        private IMapper _mapper = null!;
        private MapRegion _region = null!;

        [TestInitialize]
        public void Setup()
        {
            _baseLocation = Substitute.For<ILocation>();
            _baseLocation.RegionId.Returns(1);
            _npcService = Substitute.For<INpcService>();
            _regionService = Substitute.For<IMapRegionService>();
            _gameObjectBuilder = Substitute.For<IGameObjectBuilder>();
            _groundItemBuilder = Substitute.For<IGroundItemBuilder>();
            _mapper = Substitute.For<IMapper>();
            _region = new MapRegion(_baseLocation, new int[4], _npcService, _regionService, _gameObjectBuilder, _groundItemBuilder, _mapper);
        }

        [TestMethod]
        public void ForEachCreature_IteratesOverAll()
        {
            // Arrange
            var char1 = Substitute.For<ICharacter>();
            char1.Index.Returns(1);
            var npc1 = Substitute.For<INpc>();
            npc1.Index.Returns(2);

            _region.Add(char1);
            _region.Add(npc1);

            var seen = new List<ICreature>();

            // Act
            // Accessing private method via reflection for testing if necessary,
            // but MapRegion.ForEachCreature is private.
            // However, MajorUpdateTick calls it.
            _region.MajorUpdateTick();
            // Wait, MajorUpdateTick calls ContentTick, Combat.Tick, etc on EACH creature.
            // Let's just use the public methods if I can or use a private accessor.

            var method = typeof(MapRegion).GetMethod("ForEachCreature", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(_region, new object[] { (Action<ICreature>)(c => seen.Add(c)) });

            // Assert
            Assert.AreEqual(2, seen.Count);
            Assert.IsTrue(seen.Contains(char1));
            Assert.IsTrue(seen.Contains(npc1));
        }

        [TestMethod]
        public async Task ForEachCreatureAsync_IteratesOverAll()
        {
            // Arrange
            var char1 = Substitute.For<ICharacter>();
            char1.Index.Returns(1);
            var npc1 = Substitute.For<INpc>();
            npc1.Index.Returns(2);

            _region.Add(char1);
            _region.Add(npc1);

            var seen = new List<ICreature>();

            // Act
            var method = typeof(MapRegion).GetMethod("ForEachCreatureAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task)method!.Invoke(_region, new object[] { (Func<ICreature, Task>)(c => { seen.Add(c); return Task.CompletedTask; }) })!;
            await task;

            // Assert
            Assert.AreEqual(2, seen.Count);
            Assert.IsTrue(seen.Contains(char1));
            Assert.IsTrue(seen.Contains(npc1));
        }
    }
}
