using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class ContainerExtensionsTests
    {
        private ICharacter _character = null!;
        private IInventoryContainer _inventory = null!;
        private IGroundItemBuilder _groundItemBuilder = null!;
        private IGroundItemOnGround _groundItemOnGround = null!;
        private IGroundItemLocation _groundItemLocation = null!;
        private IGroundItemOptional _groundItemOptional = null!;
        private IItemBuilder _itemBuilder = null!;
        private IItemId _itemId = null!;
        private IItemOptional _itemOptional = null!;
        private ILootGenerator _lootGenerator = null!;

        private void UseInventory(int capacity)
        {
            _inventory = new TestInventory(capacity);
            _character.Inventory.Returns(_inventory);
        }

        private static IItem CreateItem(int id)
        {
            var item = Substitute.For<IItem>();
            item.Id.Returns(id);
            item.Count.Returns(1);
            var definition = Substitute.For<IItemDefinition>();
            definition.Stackable.Returns(true);
            item.ItemDefinition.Returns(definition);
            item.Clone().Returns(item);
            item.Clone(Arg.Any<int>()).Returns(item);
            item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call => ReferenceEquals(item, call.ArgAt<IItem>(0)));
            return item;
        }

        private sealed class TestInventory(int capacity) : IInventoryContainer
        {
            public IItemContainer Items { get; } = new ItemContainer(StorageType.Normal, capacity);
            public IItem? this[int index] => Items[index];
            public int Capacity => Items.Capacity;
            public IEnumerator<IItem?> GetEnumerator() => Items.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            public bool DropItem(IItem item) => false;
        }

        [TestInitialize]
        public void Initialize()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            _character = Substitute.For<ICharacter>();
            _inventory = new TestInventory(2);
            _groundItemBuilder = Substitute.For<IGroundItemBuilder>();
            _groundItemOnGround = Substitute.For<IGroundItemOnGround>();
            _groundItemLocation = Substitute.For<IGroundItemLocation>();
            _groundItemOptional = Substitute.For<IGroundItemOptional>();
            _itemBuilder = Substitute.For<IItemBuilder>();
            _itemId = Substitute.For<IItemId>();
            _itemOptional = Substitute.For<IItemOptional>();
            _lootGenerator = Substitute.For<ILootGenerator>();

            _character.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IGroundItemBuilder)).Returns(_groundItemBuilder);
            serviceProvider.GetService(typeof(IItemBuilder)).Returns(_itemBuilder);
            serviceProvider.GetService(typeof(ILootGenerator)).Returns(_lootGenerator);

            var groundItem = Substitute.For<IGroundItem>();

            _character.Location.Returns(new Location(1, 2, 3, 0));

            _groundItemBuilder.Create().ReturnsForAnyArgs(_groundItemOnGround);
            _groundItemOnGround.WithItem(Arg.Any<IItem>()).ReturnsForAnyArgs(_groundItemLocation);
            _groundItemLocation.WithLocation(Arg.Any<ILocation>()).ReturnsForAnyArgs(_groundItemOptional);
            _groundItemOptional.WithOwner(Arg.Any<ICharacter>()).ReturnsForAnyArgs(_groundItemOptional);
            _groundItemOptional.Spawn().ReturnsForAnyArgs(groundItem);

            _itemBuilder.Create().Returns(_itemId);
            _itemId.WithId(Arg.Any<int>()).Returns(_itemOptional);
            _itemOptional.WithCount(Arg.Any<int>()).Returns(_itemOptional);
        }

        [TestMethod]
        public void TryAddItems_WithSpaceInInventory_ShouldAddAllItems()
        {
            // Arrange
            var items = new List<IItem> { CreateItem(1), CreateItem(2) };
            UseInventory(2);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            Assert.AreEqual(2, _inventory.Items.TakenSlots);
            _groundItemOptional.DidNotReceive().Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(items, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithFullInventory_ShouldDropAllItems()
        {
            // Arrange
            var items = new List<IItem> { CreateItem(1), CreateItem(2) };
            UseInventory(0);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            Assert.AreEqual(0, _inventory.Items.TakenSlots);
            _groundItemOptional.Received(2).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(items, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithPartialSpaceInInventory_ShouldAddAndDropItems()
        {
            // Arrange
            var items = new List<IItem> { CreateItem(1), CreateItem(2) };
            UseInventory(1);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            Assert.AreEqual(1, _inventory.Items.TakenSlots);
            _groundItemOptional.Received(1).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(items, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithItemIdAndCount_WithSpaceInInventory_ShouldAddAllItems()
        {
            // Arrange
            var items = new List<(int, int)> { (1, 1), (2, 1) };
            var builtItems = new List<IItem> { CreateItem(1), CreateItem(2) };
            UseInventory(2);
            _itemOptional.Build().Returns(builtItems[0], builtItems[1]);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _itemOptional.Received(2).Build();
            Assert.AreEqual(2, _inventory.Items.TakenSlots);
            _groundItemOptional.DidNotReceive().Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(builtItems, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithItemIdAndCount_WithFullInventory_ShouldDropAllItems()
        {
            // Arrange
            var items = new List<(int, int)> { (1, 1), (2, 1) };
            var builtItems = new List<IItem> { CreateItem(1), CreateItem(2) };
            UseInventory(0);
            _itemOptional.Build().Returns(builtItems[0], builtItems[1]);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _itemOptional.Received(2).Build();
            Assert.AreEqual(0, _inventory.Items.TakenSlots);
            _groundItemOptional.Received(2).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(builtItems, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithItemIdAndCount_WithPartialSpaceInInventory_ShouldAddAndDropItems()
        {
            // Arrange
            var items = new List<(int, int)> { (1, 1), (2, 1) };
            var builtItems = new List<IItem> { CreateItem(1), CreateItem(2) };
            UseInventory(1);
            _itemOptional.Build().Returns(builtItems[0], builtItems.Last());

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _itemOptional.Received(2).Build();
            Assert.AreEqual(1, _inventory.Items.TakenSlots);
            _groundItemOptional.Received(1).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(builtItems, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddLoot_WithLootTable_WithSpaceInInventory_ShouldAddAllItems()
        {
            // Arrange
            var lootTable = Substitute.For<ILootTable>();
            var lootItem = Substitute.For<ILootItem>();
            lootItem.Id.Returns(1);
            var lootResults = new List<LootResult<ILootItem>> { new LootResult<ILootItem>(lootItem, 1) };
            var builtItem = CreateItem(1);
            _lootGenerator.GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>()).Returns(lootResults);
            UseInventory(1);
            _itemOptional.Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootTable, out var addedItems);

            // Assert
            _lootGenerator.Received(1).GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>());
            Assert.AreEqual(1, _inventory.Items.TakenSlots);
            _groundItemOptional.DidNotReceive().Spawn();
            Assert.AreEqual(1, addedItems.Count());
            Assert.AreEqual(builtItem, addedItems.First());
        }

        [TestMethod]
        public void TryAddLoot_WithLootTable_WithFullInventory_ShouldDropAllItems()
        {
            // Arrange
            var lootTable = Substitute.For<ILootTable>();
            var lootItem = Substitute.For<ILootItem>();
            lootItem.Id.Returns(1);
            var lootResults = new List<LootResult<ILootItem>> { new LootResult<ILootItem>(lootItem, 1) };
            var builtItem = CreateItem(1);
            _lootGenerator.GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>()).Returns(lootResults);
            UseInventory(0);
            _itemOptional.Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootTable, out var addedItems);

            // Assert
            _lootGenerator.Received(1).GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>());
            Assert.AreEqual(0, _inventory.Items.TakenSlots);
            _groundItemOptional.Received(1).Spawn();
            Assert.AreEqual(1, addedItems.Count());
            Assert.AreEqual(builtItem, addedItems.First());
        }

        [TestMethod]
        public void TryAddLoot_WithLootResults_WithSpaceInInventory_ShouldAddAllItems()
        {
            // Arrange
            var lootItem = Substitute.For<ILootItem>();
            lootItem.Id.Returns(1);
            var lootResults = new List<LootResult<ILootItem>> { new LootResult<ILootItem>(lootItem, 1) };
            var builtItem = CreateItem(1);
            UseInventory(1);
            _itemOptional.Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootResults, out var addedItems);

            // Assert
            Assert.AreEqual(1, _inventory.Items.TakenSlots);
            _groundItemOptional.DidNotReceive().Spawn();
            Assert.AreEqual(1, addedItems.Count());
            Assert.AreEqual(builtItem, addedItems.First());
        }

        [TestMethod]
        public void TryAddLoot_WithLootResults_WithFullInventory_ShouldDropAllItems()
        {
            // Arrange
            var lootItem = Substitute.For<ILootItem>();
            lootItem.Id.Returns(1);
            var lootResults = new List<LootResult<ILootItem>> { new LootResult<ILootItem>(lootItem, 1) };
            var builtItem = CreateItem(1);
            UseInventory(0);
            _itemOptional.Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootResults, out var addedItems);

            // Assert
            Assert.AreEqual(0, _inventory.Items.TakenSlots);
            _groundItemOptional.Received(1).Spawn();
            Assert.AreEqual(1, addedItems.Count());
            Assert.AreEqual(builtItem, addedItems.First());
        }

        [TestMethod]
        public void TryAddLoot_WithLootResults_WithPartialSpaceInInventory_ShouldAddAndDropItems()
        {
            // Arrange
            var lootItem1 = Substitute.For<ILootItem>();
            lootItem1.Id.Returns(1);
            var lootItem2 = Substitute.For<ILootItem>();
            lootItem2.Id.Returns(2);
            var lootResults = new List<LootResult<ILootItem>> { new LootResult<ILootItem>(lootItem1, 1), new LootResult<ILootItem>(lootItem2, 1) };
            var builtItems = new List<IItem> { CreateItem(1), CreateItem(2) };
            UseInventory(1);
            _itemOptional.Build().Returns(builtItems[0], builtItems[1]);

            // Act
            _inventory.TryAddLoot(_character, lootResults, out var addedItems);

            // Assert
            Assert.AreEqual(1, _inventory.Items.TakenSlots);
            _groundItemOptional.Received(1).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(builtItems, addedItems.ToList());
        }
    }
}
