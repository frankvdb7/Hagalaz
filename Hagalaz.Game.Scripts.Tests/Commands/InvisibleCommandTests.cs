using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Glow;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class InvisibleCommandTests
    {
        [TestMethod]
        public async Task Execute_TogglesVisibilityAndQueuesGlowForModerator()
        {
            // Arrange
            var glowBuilderMock = Substitute.For<IGlowBuilder>();
            var glowOptionalMock = Substitute.For<IGlowOptional>();
            var glowBuildMock = Substitute.For<IGlowBuild>();
            var glowMock = Substitute.For<IGlow>();

            glowBuilderMock.Create().Returns(glowOptionalMock);
            glowOptionalMock.WithRed(Arg.Any<byte>()).Returns(glowOptionalMock);
            glowOptionalMock.WithGreen(Arg.Any<byte>()).Returns(glowOptionalMock);
            glowOptionalMock.WithBlue(Arg.Any<byte>()).Returns(glowOptionalMock);
            glowOptionalMock.WithAlpha(Arg.Any<byte>()).Returns(glowOptionalMock);
            ((IGlowBuild)glowOptionalMock).Build().Returns(glowMock);

            var appearanceMock = Substitute.For<ICharacterAppearance>();
            appearanceMock.Visible.Returns(true);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.Appearance.Returns(appearanceMock);
            characterMock.Permissions.Returns(Permission.GameModerator);

            var command = new InvisibleCommand(glowBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "invisible" });

            // Act
            await command.Execute(args);

            // Assert
            Assert.IsFalse(appearanceMock.Visible);
            characterMock.Received(1).QueueGlow(glowMock);
        }
    }
}
