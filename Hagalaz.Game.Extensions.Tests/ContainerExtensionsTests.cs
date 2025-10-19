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
using Hagalaz.Game.Abstractions.Builders.Item;
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
        private ILootGenerator _lootGenerator = null!;

        [TestInitialize]
        public void Initialize()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            _character = Substitute.For<ICharacter>();
            _inventory = Substitute.For<IInventoryContainer>();
            _groundItemBuilder = Substitute.For<IGroundItemBuilder>();
            _groundItemOnGround = Substitute.For<IGroundItemOnGround>();
            _groundItemLocation = Substitute.For<IGroundItemLocation>();
            _groundItemOptional = Substitute.For<IGroundItemOptional>();
            _itemBuilder = Substitute.For<IItemBuilder>();
            _lootGenerator = Substitute.For<ILootGenerator>();

            _character.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IGroundItemBuilder)).Returns(_groundItemBuilder);
            serviceProvider.GetService(typeof(IItemBuilder)).Returns(_itemBuilder);
            serviceProvider.GetService(typeof(ILootGenerator)).Returns(_lootGenerator);

            var groundItem = Substitute.For<IGroundItem>();

            _character.Location.Returns(new Location(1, 2, 3, 0));
            serviceProvider.GetService(typeof(IGroundItemBuilder)).Returns(_groundItemBuilder);

            _groundItemBuilder.Create().ReturnsForAnyArgs(_groundItemOnGround);
            _groundItemOnGround.WithItem(Arg.Any<IItem>()).ReturnsForAnyArgs(_groundItemLocation);
            _groundItemLocation.WithLocation(Arg.Any<ILocation>()).ReturnsForAnyArgs(_groundItemOptional);
            _groundItemOptional.WithOwner(Arg.Any<ICharacter>()).ReturnsForAnyArgs(_groundItemOptional);
            _groundItemOptional.Spawn().ReturnsForAnyArgs(groundItem);
        }

        [TestMethod]
        public void TryAddItems_WithSpaceInInventory_ShouldAddAllItems()
        {
            // Arrange
            var items = new List<IItem> { Substitute.For<IItem>(), Substitute.For<IItem>() };
            _inventory.Add(Arg.Any<IItem>()).Returns(true);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _inventory.Received(2).Add(Arg.Any<IItem>());
            _groundItemOptional.DidNotReceive().Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(items, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithFullInventory_ShouldDropAllItems()
        {
            // Arrange
            var items = new List<IItem> { Substitute.For<IItem>(), Substitute.For<IItem>() };
            _inventory.Add(Arg.Any<IItem>()).Returns(false);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _inventory.Received(2).Add(Arg.Any<IItem>());
            _groundItemOptional.Received(2).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(items, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithPartialSpaceInInventory_ShouldAddAndDropItems()
        {
            // Arrange
            var items = new List<IItem> { Substitute.For<IItem>(), Substitute.For<IItem>() };
            _inventory.Add(Arg.Any<IItem>()).Returns(true, false);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _inventory.Received(2).Add(Arg.Any<IItem>());
            _groundItemOptional.Received(1).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(items, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithItemIdAndCount_WithSpaceInInventory_ShouldAddAllItems()
        {
            // Arrange
            var items = new List<(int, int)> { (1, 1), (2, 1) };
            var builtItems = new List<IItem> { Substitute.For<IItem>(), Substitute.For<IItem>() };
            _inventory.Add(Arg.Any<IItem>()).Returns(true);
            _itemBuilder.Create().WithId(Arg.Any<int>()).WithCount(Arg.Any<int>()).Build().Returns(builtItems[0], builtItems[1]);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _itemBuilder.Received(2).Build();
            _inventory.Received(2).Add(Arg.Any<IItem>());
            _groundItemOptional.DidNotReceive().Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(builtItems, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithItemIdAndCount_WithFullInventory_ShouldDropAllItems()
        {
            // Arrange
            var items = new List<(int, int)> { (1, 1), (2, 1) };
            var builtItems = new List<IItem> { Substitute.For<IItem>(), Substitute.For<IItem>() };
            _inventory.Add(Arg.Any<IItem>()).Returns(false);
            _itemBuilder.Create().WithId(Arg.Any<int>()).WithCount(Arg.Any<int>()).Build().Returns(builtItems[0], builtItems[1]);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _itemBuilder.Received(2).Build();
            _inventory.Received(2).Add(Arg.Any<IItem>());
            _groundItemOptional.Received(2).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(builtItems, addedItems.ToList());
        }

        [TestMethod]
        public void TryAddItems_WithItemIdAndCount_WithPartialSpaceInInventory_ShouldAddAndDropItems()
        {
            // Arrange
            var items = new List<(int, int)> { (1, 1), (2, 1) };
            var builtItems = new List<IItem> { Substitute.For<IItem>(), Substitute.For<IItem>() };
            _inventory.Add(Arg.Any<IItem>()).Returns(true, false);
            _itemBuilder.Create().WithId(Arg.Any<int>()).WithCount(Arg.Any<int>()).Build().Returns(builtItems[0], builtItems[1]);

            // Act
            _inventory.TryAddItems(_character, items, out var addedItems);

            // Assert
            _itemBuilder.Received(2).Build();
            _inventory.Received(2).Add(Arg.Any<IItem>());
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
            var builtItem = Substitute.For<IItem>();
            _lootGenerator.GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>()).Returns(lootResults);
            _inventory.Add(Arg.Any<IItem>()).Returns(true);
            _itemBuilder.Create().WithId(1).WithCount(1).Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootTable, out var addedItems);

            // Assert
            _lootGenerator.Received(1).GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>());
            _inventory.Received(1).Add(Arg.Any<IItem>());
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
            var builtItem = Substitute.For<IItem>();
            _lootGenerator.GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>()).Returns(lootResults);
            _inventory.Add(Arg.Any<IItem>()).Returns(false);
            _itemBuilder.Create().WithId(1).WithCount(1).Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootTable, out var addedItems);

            // Assert
            _lootGenerator.Received(1).GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>());
            _inventory.Received(1).Add(Arg.Any<IItem>());
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
            var builtItem = Substitute.For<IItem>();
            _inventory.Add(Arg.Any<IItem>()).Returns(true);
            _itemBuilder.Create().WithId(1).WithCount(1).Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootResults, out var addedItems);

            // Assert
            _inventory.Received(1).Add(Arg.Any<IItem>());
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
            var builtItem = Substitute.For<IItem>();
            _inventory.Add(Arg.Any<IItem>()).Returns(false);
            _itemBuilder.Create().WithId(1).WithCount(1).Build().Returns(builtItem);

            // Act
            _inventory.TryAddLoot(_character, lootResults, out var addedItems);

            // Assert
            _inventory.Received(1).Add(Arg.Any<IItem>());
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
            var builtItems = new List<IItem> { Substitute.For<IItem>(), Substitute.For<IItem>() };
            _inventory.Add(Arg.Any<IItem>()).Returns(true, false);
            _itemBuilder.Create().WithId(Arg.Any<int>()).WithCount(Arg.Any<int>()).Build().Returns(builtItems[0], builtItems[1]);

            // Act
            _inventory.TryAddLoot(_character, lootResults, out var addedItems);

            // Assert
            _inventory.Received(2).Add(Arg.Any<IItem>());
            _groundItemOptional.Received(1).Spawn();
            Assert.AreEqual(2, addedItems.Count());
            CollectionAssert.AreEquivalent(builtItems, addedItems.ToList());
        }
    }
}
