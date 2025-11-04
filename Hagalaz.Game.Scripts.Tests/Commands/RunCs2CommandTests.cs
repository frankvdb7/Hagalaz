using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class RunCs2CommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsCs2Script()
        {
            // Arrange
            var configurationsMock = Substitute.For<ICharacterConfigurations>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Configurations.Returns(configurationsMock);

            var command = new RunCs2Command();
            var args = new GameCommandArgs(characterMock, new[] { "si", "1", "hello", "2" });

            // Act
            await command.Execute(args);

            // Assert
            configurationsMock.Received(1).SendCs2Script(1, Arg.Is<object[]>(o => (string)o[0] == "hello" && (int)o[1] == 2));
        }
    }
}
