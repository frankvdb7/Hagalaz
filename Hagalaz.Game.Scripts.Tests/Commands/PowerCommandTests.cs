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
    public class PowerCommandTests
    {
        [TestMethod]
        public async Task Execute_SetsPowerBonuses()
        {
            // Arrange
            var bonusesMock = Substitute.For<ICharacterBonuses>();
            var statisticsMock = Substitute.For<ICharacterStatistics>();
            statisticsMock.Bonuses.Returns(bonusesMock);
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Statistics.Returns(statisticsMock);

            var command = new PowerCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            bonusesMock.Received(1).SetBonus(BonusType.Strength, 2000);
            bonusesMock.Received(1).SetBonus(BonusType.RangedStrength, 2000);
            bonusesMock.Received(1).SetBonus(BonusType.MagicDamage, 2000);
        }
    }
}
