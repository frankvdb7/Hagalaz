using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SetSkullCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidIcon_SetsSkullIcon()
        {
            // Arrange
            var appearanceMock = Substitute.For<ICharacterAppearance>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Appearance.Returns(appearanceMock);

            var command = new SetSkullCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1" });

            // Act
            await command.Execute(args);

            // Assert
            appearanceMock.Received(1).SkullIcon = (SkullIcon)1;
        }

        [TestMethod]
        public async Task Execute_WithInvalidIcon_DoesNotSetSkullIcon()
        {
            // Arrange
            var appearanceMock = Substitute.For<ICharacterAppearance>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Appearance.Returns(appearanceMock);

            var command = new SetSkullCommand();
            var args = new GameCommandArgs(characterMock, new[] { "invalid" });

            // Act
            await command.Execute(args);

            // Assert
            appearanceMock.DidNotReceive().SkullIcon = Arg.Any<SkullIcon>();
        }

        [TestMethod]
        public async Task Execute_WithNoArguments_DoesNotSetSkullIcon()
        {
            // Arrange
            var appearanceMock = Substitute.For<ICharacterAppearance>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Appearance.Returns(appearanceMock);

            var command = new SetSkullCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            appearanceMock.DidNotReceive().SkullIcon = Arg.Any<SkullIcon>();
        }
    }
}
