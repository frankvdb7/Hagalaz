using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class AddExpCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_AddsExperience()
        {
            // Arrange
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new AddExpCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "100.5" });

            // Act
            await command.Execute(args);

            // Assert
            statisticsMock.Received(1).AddExperience(1, 100.5);
        }

        [TestMethod]
        public async Task Execute_WithInvalidSkillId_DoesNotAddExperience()
        {
            // Arrange
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new AddExpCommand();
            var args = new GameCommandArgs(characterMock, new[] { "invalid", "100.5" });

            // Act
            await command.Execute(args);

            // Assert
            statisticsMock.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
        }

        [TestMethod]
        public async Task Execute_WithInvalidExperience_DoesNotAddExperience()
        {
            // Arrange
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new AddExpCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "invalid" });

            // Act
            await command.Execute(args);

            // Assert
            statisticsMock.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
        }

        [TestMethod]
        public async Task Execute_WithMissingArguments_DoesNotAddExperienceAndSendsMessage()
        {
            // Arrange
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new AddExpCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1" });

            // Act
            await command.Execute(args);

            // Assert
            statisticsMock.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
            characterMock.Received(1).SendChatMessage("Invalid syntax. Use: addexp [skillId] [experience]");
        }
    }
}
