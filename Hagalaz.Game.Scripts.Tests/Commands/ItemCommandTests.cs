using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class ItemCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_AddsItemToInventory()
        {
            // Arrange
            var inventoryMock = Substitute.For<IItemContainer>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Inventory.Returns(inventoryMock);

            var command = new ItemCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "2" });

            // Act
            await command.Execute(args);

            // Assert
            inventoryMock.Received(1).Add(Arg.Is<IItem>(i => i.Id == 1 && i.Count == 2));
        }
    }
}
