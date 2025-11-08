using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class VoiceCommandTests
    {
        [TestMethod]
        public async Task Execute_DoesNotThrowException()
        {
            // Arrange
            var soundBuilderMock = Substitute.For<Hagalaz.Game.Abstractions.Builders.Audio.IAudioBuilder>();
            var characterMock = Substitute.For<ICharacter>();
            var command = new VoiceCommand(soundBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "voice", "123" });

            // Act
            await command.Execute(args);

            // Assert - no exception thrown
        }
    }
}
