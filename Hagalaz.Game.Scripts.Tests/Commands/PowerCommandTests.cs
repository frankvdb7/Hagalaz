using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class PowerCommandTests
    {
        [TestMethod]
        public async Task Execute_SetsPowerBonuses()
        {
            // Arrange
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new PowerCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            var bonusContainer = statisticsMock.Bonuses;
            bonusContainer.Received(1).SetBonus(BonusType.Strength, 2000);
            bonusContainer.Received(1).SetBonus(BonusType.RangedStrength, 2000);
            bonusContainer.Received(1).SetBonus(BonusType.MagicDamage, 2000);
        }
    }
}
