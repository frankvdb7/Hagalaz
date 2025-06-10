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
            var groundItemBuilder = new SimpleGroundItemBuilder(publicTicks);
            var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
            var location = Location.Create(0, 0);
            return new MapRegion(
                location,
                new int[4],
                npcService.Object,
                regionService.Object,
                gameObjectBuilder.Object,
                groundItemBuilder,
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

            var items = region.FindAllGroundItems().ToList();
            Assert.AreEqual(1, items.Count);
            Assert.AreNotSame(item, items[0]);
            var respawnItem = items[0];
            Assert.IsTrue(respawnItem.IsRespawning);
            Assert.AreEqual(respawnItem.RespawnTicks, respawnItem.TicksLeft);
        }

        [TestMethod]
        public async Task Tick_Respawns_Item_When_Timer_Expires()
        {
            var region = CreateRegion();
            var item = CreateItem(2, 1);
            region.Add(item);

            // First tick: item timer expires, should be replaced by a respawning item
            await region.MajorClientPrepareUpdateTick();
            var respawning = region.FindAllGroundItems().Single();
            Assert.AreNotSame(item, respawning, "Item should be replaced by a respawning item");
            Assert.IsTrue(respawning.IsRespawning, "Item should be in respawning state after first tick");
            Assert.AreEqual(2, respawning.TicksLeft, "Respawning item should have its respawn ticks reset");

            // Second tick: respawning item timer decrements
            await region.MajorClientPrepareUpdateTick();
            var afterSecondTick = region.FindAllGroundItems().Single();
            // Log state for debug
            Console.WriteLine($"After 2nd tick: IsRespawning={afterSecondTick.IsRespawning}, TicksLeft={afterSecondTick.TicksLeft}");
            Assert.AreEqual(1, afterSecondTick.TicksLeft, "Respawning item ticks should decrement");
            Assert.IsTrue(afterSecondTick.IsRespawning, "Item should still be respawning");

            // Third tick: respawning item should become a normal item again
            await region.MajorClientPrepareUpdateTick();
            var items = region.FindAllGroundItems().ToList();
            foreach (var i in items)
                Console.WriteLine($"After 3rd tick: IsRespawning={i.IsRespawning}, TicksLeft={i.TicksLeft}");
            Assert.AreEqual(1, items.Count, "There should be one item after respawn");
            var respawned = items[0];
            Assert.IsFalse(respawned.IsRespawning, $"Item should no longer be respawning after respawn, but was: {respawned.IsRespawning}, TicksLeft={respawned.TicksLeft}");
            Assert.AreEqual(respawned.RespawnTicks, respawned.TicksLeft, "Respawned item should have its ticks reset");
        }

        [TestMethod]
        public async Task Tick_Makes_Private_Item_Public_When_Timer_Expires()
        {
            var region = CreateRegion();
            var item = CreatePrivateItem(true, 1);
            region.Add(item);

            await region.MajorClientPrepareUpdateTick();

            var items = region.FindAllGroundItems().ToList();
            Assert.AreEqual(1, items.Count);
            Assert.AreNotSame(item, items[0]);
            Assert.IsNull(items[0].Owner);
        }

        [TestMethod]
        public void CanDestroy_Returns_False_When_Respawning()
        {
            var item = CreateItem(5, 0);
            var respawning = new GroundItem(item.ItemOnGround, item.Location, null, item.RespawnTicks, 0, true);

            Assert.IsFalse(respawning.CanDestroy());
        }

        [TestMethod]
        public void CanDestroy_Returns_True_When_Ticks_Expired()
        {
            var item = CreateItem(0, 0);

            Assert.IsTrue(item.CanDestroy());
        }

        public class SimpleGroundItemBuilder : IGroundItemBuilder, IGroundItemOnGround, IGroundItemLocation, IGroundItemOptional, IGroundItemBuild {
            private int? _respawnTicks;
            private int? _ticks;
            private readonly int _publicTicks;
            private IItem _item = default!;
            private ILocation _location = default!;
            private ICharacter? _owner;
            private bool _isRespawning;

            public SimpleGroundItemBuilder(int publicTicks = 100)
            {
                _publicTicks = publicTicks;
            }

            // IGroundItemBuilder
            public IGroundItemOnGround Create() => new SimpleGroundItemBuilder(_publicTicks);

            // IGroundItemOnGround
            public IGroundItemLocation WithItem(IItem item) { _item = item; return this; }
            public IGroundItemLocation WithItem(Func<IItemBuilder, IItemBuild> itemBuilder) { return this; }

            // IGroundItemLocation
            public IGroundItemOptional WithLocation(ILocation location) { _location = location; return this; }

            // IGroundItemOptional
            public IGroundItemOptional WithOwner(ICharacter owner) { _owner = owner; return this; }
            public IGroundItemOptional WithRespawnTicks(int respawnTicks) { _respawnTicks = respawnTicks; return this; }
            public IGroundItemOptional WithTicks(int ticks) { _ticks = ticks; return this; }
            public IGroundItemOptional AsRespawning() { _isRespawning = true; return this; }

            // IGroundItemBuild
            public IGroundItem Build()
            {
                var defaultTicks = _publicTicks;
                var respawnTicks = _respawnTicks ?? defaultTicks;
                int ticks;
                if (_ticks.HasValue)
                {
                    ticks = _ticks.Value;
                }
                else if (_respawnTicks.HasValue)
                {
                    ticks = _respawnTicks.Value == 0 ? defaultTicks : _respawnTicks.Value;
                }
                else
                {
                    ticks = respawnTicks;
                }

                var result = new GroundItem(_item, _location, _owner, respawnTicks, ticks, _isRespawning);

                return result;
            }
            public IGroundItem Spawn() => Build();
        }
    }
}
