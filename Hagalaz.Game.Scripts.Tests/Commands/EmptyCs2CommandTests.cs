using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class EmptyCs2CommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsEmptyCs2Script()
        {
            // Arrange
            var configurationsMock = Substitute.For<ICharacterConfigurations>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Configurations.Returns(configurationsMock);

            var command = new EmptyCs2Command();
            var args = new GameCommandArgs(characterMock, new[] { "1" });

            // Act
            await command.Execute(args);

            // Assert
            configurationsMock.Received(1).SendCs2Script(1, Arg.Is<object[]>(o => o.Length == 0));
        }
    }
}
