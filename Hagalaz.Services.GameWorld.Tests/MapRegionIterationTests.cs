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
using System;
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
        public void ForEachCharacter_IteratesCorrectly()
        {
            var char1 = Substitute.For<ICharacter>();
            char1.Index.Returns(1);
            _region.Add(char1);

            var seen = new List<ICharacter>();
            _region.ForEachCharacter((c, list) => list.Add(c), seen);

            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(char1, seen[0]);
        }

        [TestMethod]
        public void ForEachNpc_IteratesCorrectly()
        {
            var npc1 = Substitute.For<INpc>();
            npc1.Index.Returns(2);
            _region.Add(npc1);

            var seen = new List<INpc>();
            _region.ForEachNpc((n, list) => list.Add(n), seen);

            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(npc1, seen[0]);
        }
    }
}
