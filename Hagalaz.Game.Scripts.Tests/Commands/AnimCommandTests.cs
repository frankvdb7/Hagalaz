using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class AnimCommandTests
    {
        private Mock<IAnimationBuilder> _animationBuilderMock = null!;
        private Mock<IAnimationId> _animationIdMock = null!;
        private Mock<IAnimationOptional> _animationOptionalMock = null!;
        private Mock<IAnimation> _animationMock = null!;
        private AnimCommand _animCommand = null!;

        [TestInitialize]
        public void Setup()
        {
            _animationBuilderMock = new Mock<IAnimationBuilder>();
            _animationIdMock = new Mock<IAnimationId>();
            _animationOptionalMock = new Mock<IAnimationOptional>();
            _animationMock = new Mock<IAnimation>();

            _animCommand = new AnimCommand(_animationBuilderMock.Object);

            _animationBuilderMock.Setup(b => b.Create()).Returns(_animationIdMock.Object);
            _animationIdMock.Setup(i => i.WithId(It.IsAny<int>())).Returns(_animationOptionalMock.Object);
            _animationOptionalMock.Setup(o => o.WithDelay(It.IsAny<int>())).Returns(_animationOptionalMock.Object);
            _animationOptionalMock.Setup(o => o.Build()).Returns(_animationMock.Object);
        }

        [TestMethod]
        public void Name_ShouldReturnCorrectName()
        {
            // Assert
            Assert.AreEqual("anim", _animCommand.Name);
        }

        [TestMethod]
        public void Permission_ShouldReturnCorrectPermission()
        {
            // Assert
            Assert.AreEqual(Permission.SystemAdministrator, _animCommand.Permission);
        }

        [TestMethod]
        public async Task Execute_WithId_ShouldQueueAnimationWithCorrectIdAndZeroDelay()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var args = new GameCommandArgs(characterMock.Object, new[] { "anim", "123" });

            // Act
            await _animCommand.Execute(args);

            // Assert
            _animationIdMock.Verify(i => i.WithId(123), Times.Once);
            _animationOptionalMock.Verify(o => o.WithDelay(0), Times.Once);
            characterMock.Verify(c => c.QueueAnimation(_animationMock.Object), Times.Once);
        }

        [TestMethod]
        public async Task Execute_WithIdAndDelay_ShouldQueueAnimationWithCorrectIdAndDelay()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var args = new GameCommandArgs(characterMock.Object, new[] { "anim", "123", "45" });

            // Act
            await _animCommand.Execute(args);

            // Assert
            _animationIdMock.Verify(i => i.WithId(123), Times.Once);
            _animationOptionalMock.Verify(o => o.WithDelay(45), Times.Once);
            characterMock.Verify(c => c.QueueAnimation(_animationMock.Object), Times.Once);
        }
    }
}