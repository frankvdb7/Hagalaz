using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Sound;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SoundCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsSound()
        {
            // Arrange
            var audioBuilderMock = Substitute.For<IAudioBuilder>();
            var soundTypeMock = Substitute.For<ISoundType>();
            var soundIdMock = Substitute.For<ISoundId>();
            var soundOptionalMock = Substitute.For<ISoundOptional>();
            var soundBuildMock = Substitute.For<ISoundBuild>();
            var audioMock = Substitute.For<ISound>();

            audioBuilderMock.Create().Returns(soundTypeMock);
            soundTypeMock.AsSound().Returns(soundIdMock);
            soundIdMock.WithId(Arg.Any<int>()).Returns(soundOptionalMock);
            soundOptionalMock.WithVolume(Arg.Any<int>()).Returns(soundOptionalMock);
            soundOptionalMock.WithDelay(Arg.Any<int>()).Returns(soundOptionalMock);
            soundOptionalMock.WithPlaybackSpeed(Arg.Any<int>()).Returns(soundOptionalMock);
            soundOptionalMock.WithRepeatCount(Arg.Any<int>()).Returns(soundOptionalMock);
            ((ISoundBuild)soundOptionalMock).Build().Returns(audioMock);

            var sessionMock = Substitute.For<IGameSession>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Session.Returns(sessionMock);

            var command = new SoundCommand(audioBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "sound", "123" });

            // Act
            await command.Execute(args);

            // Assert
            sessionMock.Received(1).SendMessage(audioMock.ToMessage());
        }
    }
}
