using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class ToPlayerCommandTests
    {
        [TestMethod]
        public async Task Execute_TurnsToPlayer()
        {
            // Arrange
            var appearanceMock = Substitute.For<ICharacterAppearance>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Appearance.Returns(appearanceMock);

            var command = new ToPlayerCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            appearanceMock.Received(1).TurnToPlayer();
        }
    }
}
