using Hagalaz.Game.Abstractions.Builders;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
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

            _character.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IGroundItemBuilder)).Returns(_groundItemBuilder);

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
    }
}
