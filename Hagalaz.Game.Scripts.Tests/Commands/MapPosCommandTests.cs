using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class MapPosCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsMapPositionMessage()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Location.Returns(new Location(0, 0, 0, 0));

            var command = new MapPosCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "2" });

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).SendChatMessage(Arg.Is<string>(s => s.StartsWith("X:") && s.Contains("Y:")));
        }
    }
}
