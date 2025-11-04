using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class LevelCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SetsSkillExperience()
        {
            // Arrange
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new LevelCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "99" });

            // Act
            await command.Execute(args);

            // Assert
            statisticsMock.Received(1).SetSkillExperience(1, StatisticsHelpers.ExperienceForLevel(99));
        }
    }
}
