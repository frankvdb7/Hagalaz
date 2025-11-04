using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class DefenceCommandTests
    {
        [TestMethod]
        public async Task Execute_SetsDefenceBonuses()
        {
            // Arrange
            var bonusesMock = Substitute.For<ICharacterBonuses>();
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            statisticsMock.Bonuses.Returns(bonusesMock);
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new DefenceCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            bonusesMock.Received(1).SetBonus(BonusType.DefenceStab, 2000);
            bonusesMock.Received(1).SetBonus(BonusType.DefenceRanged, 2000);
            bonusesMock.Received(1).SetBonus(BonusType.DefenceMagic, 2000);
            bonusesMock.Received(1).SetBonus(BonusType.DefenceCrush, 2000);
            bonusesMock.Received(1).SetBonus(BonusType.DefenceSummoning, 2000);
            bonusesMock.Received(1).SetBonus(BonusType.DefenceSlash, 2000);
        }
    }
}
