using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class ItemCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_AddsItemToInventory()
        {
            // Arrange
            var itemBuilderMock = Substitute.For<IItemBuilder>();
            var itemIdMock = Substitute.For<IItemId>();
            var itemOptionalMock = Substitute.For<IItemOptional>();
            var itemBuildMock = Substitute.For<IItemBuild>();
            var itemMock = Substitute.For<IItem>();

            itemBuilderMock.Create().Returns(itemIdMock);
            itemIdMock.WithId(123).Returns(itemOptionalMock);
            itemOptionalMock.WithCount(456).Returns(itemOptionalMock);
            ((IItemBuild)itemOptionalMock).Build().Returns(itemMock);

            var inventoryMock = Substitute.For<IInventoryContainer>();

            var characterMock = Substitute.For<ICharacter>();
            characterMock.Inventory.Returns(inventoryMock);

            var command = new ItemCommand(itemBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "item", "123", "456" });

            // Act
            await command.Execute(args);

            // Assert
            inventoryMock.Received(1).Add(itemMock);
        }
    }
}
