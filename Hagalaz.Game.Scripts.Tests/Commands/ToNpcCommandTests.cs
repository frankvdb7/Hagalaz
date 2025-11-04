using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class ToNpcCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_TurnsToNpc()
        {
            // Arrange
            var appearanceMock = Substitute.For<ICharacterAppearance>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Appearance.Returns(appearanceMock);

            var command = new ToNpcCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1" });

            // Act
            await command.Execute(args);

            // Assert
            appearanceMock.Received(1).TurnToNpc(1);
        }
    }
}
