using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Services.GameWorld.Model.Items;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Hagalaz.Game.Configuration;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class GroundItemTickTests
    {
        private static MapRegion CreateRegion(int publicTicks = 100)
        {
            var npcService = new Mock<INpcService>();
            var regionService = new Mock<IMapRegionService>();
            var gameObjectBuilder = new Mock<IGameObjectBuilder>();
            var groundItemBuilder = new SimpleGroundItemBuilder();
            var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
            var location = Location.Create(0, 0);
            var options = Options.Create(new GroundItemOptions { PublicTickTime = publicTicks });
            return new MapRegion(
                location,
                new int[4],
                npcService.Object,
                regionService.Object,
                gameObjectBuilder.Object,
                groundItemBuilder,
                options,
                mapper);
        }

        private static IGroundItem CreateItem(int respawnTicks, int ticksLeft)
        {
            var itemScript = new Mock<IItemScript>();
            itemScript.Setup(s => s.CanTradeItem(It.IsAny<IItem>(), It.IsAny<ICharacter>())).Returns(true);
            var item = new Mock<IItem>();
            item.SetupGet(i => i.ItemScript).Returns(itemScript.Object);
            item.Setup(i => i.Clone()).Returns(item.Object);
            item.Setup(i => i.Clone(It.IsAny<int>())).Returns(item.Object);
            item.SetupGet(i => i.Name).Returns("Item");
            item.SetupGet(i => i.Count).Returns(1);
            item.SetupGet(i => i.Id).Returns(0);
            var location = Location.Create(10, 10);
            return new GroundItem(item.Object, location, null, respawnTicks, ticksLeft);
        }

        private static IGroundItem CreatePrivateItem(bool tradable, int ticksLeft)
        {
            var itemScript = new Mock<IItemScript>();
            itemScript.Setup(s => s.CanTradeItem(It.IsAny<IItem>(), It.IsAny<ICharacter>())).Returns(tradable);
            var item = new Mock<IItem>();
            item.SetupGet(i => i.ItemScript).Returns(itemScript.Object);
            item.Setup(i => i.Clone()).Returns(item.Object);
            item.Setup(i => i.Clone(It.IsAny<int>())).Returns(item.Object);
            item.SetupGet(i => i.Name).Returns("Item");
            item.SetupGet(i => i.Count).Returns(1);
            item.SetupGet(i => i.Id).Returns(0);
            var location = Location.Create(10, 10);
            var owner = new Mock<ICharacter>().Object;
            return new GroundItem(item.Object, location, owner, 0, ticksLeft);
        }

        [TestMethod]
        public async Task Tick_Decrements_Timer()
        {
            var region = CreateRegion();
            var item = CreateItem(5, 5);
            region.Add(item);

            await region.MajorClientPrepareUpdateTick();

            Assert.AreEqual(4, item.TicksLeft);
        }

        [TestMethod]
        public async Task Tick_Removes_Item_When_Timer_Expires_Without_Respawn()
        {
            var region = CreateRegion();
            var item = CreateItem(0, 1);
            region.Add(item);

            await region.MajorClientPrepareUpdateTick();

            Assert.AreEqual(0, region.FindAllGroundItems().Count());
        }

        [TestMethod]
        public async Task Tick_Decrements_Timer_While_Item_Respawning()
        {
            var region = CreateRegion();
            var item = CreateItem(2, 2);
            region.Add(item);

            await region.MajorClientPrepareUpdateTick();
            await region.MajorClientPrepareUpdateTick();

            var gi = (GroundItem)item;
            Assert.IsTrue(gi.IsRespawning);
            Assert.AreEqual(gi.RespawnTicks, gi.TicksLeft);
        }

        [TestMethod]
        public async Task Tick_Respawns_Item_When_Timer_Expires()
        {
            var region = CreateRegion();
            var item = CreateItem(2, 1);
            region.Add(item);

            await region.MajorClientPrepareUpdateTick();

            Assert.IsTrue(item.IsRespawning);
            Assert.AreEqual(2, item.TicksLeft);

            await region.MajorClientPrepareUpdateTick();
            await region.MajorClientPrepareUpdateTick();

            var items = region.FindAllGroundItems().ToList();
            Assert.AreEqual(1, items.Count);
            Assert.AreNotSame(item, items[0]);
            var respawned = (GroundItem)items[0];
            Assert.IsFalse(respawned.IsRespawning);
            Assert.AreEqual(respawned.RespawnTicks, respawned.TicksLeft);
        }

        [TestMethod]
        public async Task Tick_Makes_Private_Item_Public_When_Timer_Expires()
        {
            const int publicTicks = 42;
            var region = CreateRegion(publicTicks);
            var item = CreatePrivateItem(true, 1);
            region.Add(item);

            await region.MajorClientPrepareUpdateTick();

            var items = region.FindAllGroundItems().ToList();
            Assert.AreEqual(1, items.Count);
            Assert.AreNotSame(item, items[0]);
            Assert.IsNull(items[0].Owner);
            Assert.AreEqual(publicTicks, items[0].TicksLeft);
        }
    }

    class SimpleGroundItemBuilder : IGroundItemBuilder, IGroundItemOnGround, IGroundItemLocation, IGroundItemOptional, IGroundItemBuild
    {
        private IItem _item = default!;
        private ILocation _location = default!;
        private ICharacter? _owner;
        private int _respawnTicks;
        private int _ticks;

        public IGroundItemOnGround Create() => new SimpleGroundItemBuilder();

        public IGroundItemLocation WithItem(IItem item)
        {
            _item = item;
            return this;
        }

        public IGroundItemLocation WithItem(Func<IItemBuilder, IItemBuild> itemBuilder) => throw new NotImplementedException();

        public IGroundItemOptional WithLocation(ILocation location)
        {
            _location = location;
            return this;
        }

        public IGroundItemOptional WithOwner(ICharacter owner)
        {
            _owner = owner;
            return this;
        }

        public IGroundItemOptional WithRespawnTicks(int respawnTicks)
        {
            _respawnTicks = respawnTicks;
            return this;
        }

        public IGroundItemOptional WithTicks(int ticks)
        {
            _ticks = ticks;
            return this;
        }

        public IGroundItem Build() => new GroundItem(_item, _location, _owner, _respawnTicks, _ticks);

        public IGroundItem Spawn() => Build();
    }
}
