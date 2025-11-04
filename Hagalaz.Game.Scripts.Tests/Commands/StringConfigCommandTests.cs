using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class StringConfigCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsStringConfig()
        {
            // Arrange
            var configurationsMock = Substitute.For<ICharacterConfigurations>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Configurations.Returns(configurationsMock);

            var command = new StringConfigCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "hello" });

            // Act
            await command.Execute(args);

            // Assert
            configurationsMock.Received(1).SendGlobalCs2String(1, "hello");
        }
    }
}
