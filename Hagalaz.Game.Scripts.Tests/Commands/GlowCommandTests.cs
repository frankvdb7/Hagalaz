using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Glow;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class GlowCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_QueuesGlow()
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
            glowOptionalMock.WithDuration(Arg.Any<int>()).Returns(glowOptionalMock);
            ((IGlowBuild)glowOptionalMock).Build().Returns(glowMock);

            var characterMock = Substitute.For<ICharacter>();
            var command = new GlowCommand(glowBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "glow", "1", "2", "3", "4", "5" });

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).QueueGlow(glowMock);
        }
    }
}
