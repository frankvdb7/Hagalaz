using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SearchItemCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SearchesItems()
        {
            // Arrange
            var itemServiceMock = Substitute.For<IItemService>();
            var itemDefinitionMock = Substitute.For<IItemDefinition>();
            itemDefinitionMock.Name.Returns("Test Item");
            itemServiceMock.GetTotalItemCount().Returns(1);
            itemServiceMock.FindItemDefinitionById(0).Returns(itemDefinitionMock);

            var characterMock = Substitute.For<ICharacter>();
            var command = new SearchItemCommand(itemServiceMock);
            var args = new GameCommandArgs(characterMock, new[] { "test" });

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).SendChatMessage("0 - Test Item", ChatMessageType.ConsoleText);
        }
    }
}
