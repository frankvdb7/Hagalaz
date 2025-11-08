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
    public class MusicEffectCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsMusicEffect()
        {
            // Arrange
            var audioBuilderMock = Substitute.For<IAudioBuilder>();
            var soundTypeMock = Substitute.For<ISoundType>();
            var musicEffectIdMock = Substitute.For<IMusicEffectId>();
            var musicEffectOptionalMock = Substitute.For<IMusicEffectOptional>();
            var audioMock = Substitute.For<ISound>();

            audioBuilderMock.Create().Returns(soundTypeMock);
            soundTypeMock.AsMusicEffect().Returns(musicEffectIdMock);
            musicEffectIdMock.WithId(Arg.Any<int>()).Returns(musicEffectOptionalMock);
            musicEffectOptionalMock.Build().Returns(audioMock);

            var sessionMock = Substitute.For<IGameSession>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Session.Returns(sessionMock);

            var command = new MusicEffectCommand(audioBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "effect", "123" });

            // Act
            await command.Execute(args);

            // Assert
            sessionMock.Received(1).SendMessage(audioMock.ToMessage());
        }
    }
}
