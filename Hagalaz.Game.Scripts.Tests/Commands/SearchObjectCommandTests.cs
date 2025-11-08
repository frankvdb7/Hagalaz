using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SearchObjectCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SearchesObjects()
        {
            // Arrange
            var gameObjectServiceMock = Substitute.For<IGameObjectService>();
            var gameObjectDefinitionMock = Substitute.For<IGameObjectDefinition>();
            gameObjectDefinitionMock.Name.Returns("Test Object");
            gameObjectServiceMock.GetObjectsCount().Returns(1);
            gameObjectServiceMock.FindGameObjectDefinitionById(0).Returns(gameObjectDefinitionMock);

            var characterMock = Substitute.For<ICharacter>();
            var command = new SearchObjectCommand(gameObjectServiceMock);
            var args = new GameCommandArgs(characterMock, new[] { "test" });

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).SendChatMessage("0 - Test Object", ChatMessageType.ConsoleText);
        }
    }
}
