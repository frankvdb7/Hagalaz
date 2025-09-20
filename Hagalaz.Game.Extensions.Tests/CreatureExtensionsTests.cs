using Hagalaz.Game.Abstractions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Tasks;
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
        public void FaceLocation_CallsFaceWithCorrectLocation()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var location = Substitute.For<ILocation>();

            // Act
            creature.Face(location);

            // Assert
            creature.Received(1).Face(location);
        }

        [TestMethod]
        public void FaceCreature_CallsFaceWithCorrectCreature()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var target = Substitute.For<ICreature>();

            // Act
            creature.Face(target);

            // Assert
            creature.Received(1).Face(target);
        }

        [TestMethod]
        public void QueueTask_CallsSchedulerWithCorrectTask()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var scheduler = Substitute.For<IScheduler>();
            creature.Scheduler.Returns(scheduler);
            var task = Substitute.For<RsTask>();

            // Act
            creature.QueueTask(task);

            // Assert
            scheduler.Received(1).QueueTask(task);
        }
    }
}
