using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class CreatureExtensionsTests
    {
        [TestMethod]
        public void FaceLocation_WithGameObject_CallsFaceLocationWithCorrectParameters()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var gameObject = Substitute.For<IGameObject>();
            var location = Substitute.For<ILocation>();
            gameObject.Location.Returns(location);
            gameObject.SizeX.Returns(1);
            gameObject.SizeY.Returns(1);

            // Act
            creature.FaceLocation(gameObject);

            // Assert
            creature.Received(1).FaceLocation(location, 1, 1);
        }

        [TestMethod]
        public void FaceLocation_WithCreature_CallsFaceLocationWithCorrectParameters()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var creatureToFace = Substitute.For<ICreature>();
            var location = Substitute.For<ILocation>();
            creatureToFace.Location.Returns(location);
            creatureToFace.Size.Returns(1);

            // Act
            creature.FaceLocation(creatureToFace);

            // Assert
            creature.Received(1).FaceLocation(location, 1, 1);
        }

        [TestMethod]
        public void QueueTask_WithFunc_CallsQueueTaskWithCorrectParameters()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();

            // Act
            creature.QueueTask(() => System.Threading.Tasks.Task.CompletedTask);

            // Assert
            creature.Received(1).QueueTask(Arg.Any<RsAsyncTask>());
        }

        [TestMethod]
        public void QueueTask_WithAction_CallsQueueTaskWithCorrectParameters()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();

            // Act
            creature.QueueTask(() => { }, 1);

            // Assert
            creature.Received(1).QueueTask(Arg.Any<RsTask>());
        }
    }
}
