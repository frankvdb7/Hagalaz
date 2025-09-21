using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class CreatureExtensionsTests
    {
        [TestMethod]
        public void IsDead_WhenCombatIsDead_ReturnsTrue()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(true);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isDead = creature.IsDead();

            // Assert
            Assert.IsTrue(isDead);
        }

        [TestMethod]
        public void IsDead_WhenCombatIsAlive_ReturnsFalse()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(false);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isDead = creature.IsDead();

            // Assert
            Assert.IsFalse(isDead);
        }

        [TestMethod]
        public void IsAlive_WhenCombatIsAlive_ReturnsTrue()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(false);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isAlive = creature.IsAlive();

            // Assert
            Assert.IsTrue(isAlive);
        }

        [TestMethod]
        public void IsAlive_WhenCombatIsDead_ReturnsFalse()
        {
            // Arrange
            var combat = Substitute.For<ICreatureCombat>();
            combat.IsDead.Returns(true);
            var creature = Substitute.For<ICreature>();
            creature.Combat.Returns(combat);

            // Act
            var isAlive = creature.IsAlive();

            // Assert
            Assert.IsFalse(isAlive);
        }
    }
}
