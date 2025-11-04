using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class BitConfigCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsBitConfig()
        {
            // Arrange
            var configurationsMock = Substitute.For<ICharacterConfigurations>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Configurations.Returns(configurationsMock);

            var command = new BitConfigCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "2" });

            // Act
            await command.Execute(args);

            // Assert
            configurationsMock.Received(1).SendBitConfiguration(1, 2);
        }
    }
}
