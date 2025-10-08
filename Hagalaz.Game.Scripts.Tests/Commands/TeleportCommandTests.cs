using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Teleport;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class TeleportCommandTests
    {
        private Mock<ITeleportBuilder> _teleportBuilderMock;
        private Mock<ITeleportType> _teleportTypeMock;
        private Mock<ITeleportY> _teleportYMock;
        private Mock<ITeleportOptional> _teleportOptionalMock;
        private Mock<ITeleport> _teleportMock;
        private TeleportCommand _teleportCommand;

        [TestInitialize]
        public void Setup()
        {
            _teleportBuilderMock = new Mock<ITeleportBuilder>();
            _teleportTypeMock = new Mock<ITeleportType>();
            _teleportYMock = new Mock<ITeleportY>();
            _teleportOptionalMock = new Mock<ITeleportOptional>();
            _teleportMock = new Mock<ITeleport>();

            _teleportCommand = new TeleportCommand(_teleportBuilderMock.Object);

            _teleportBuilderMock.Setup(b => b.Create()).Returns(_teleportTypeMock.Object);
            _teleportTypeMock.Setup(t => t.WithX(It.IsAny<int>())).Returns(_teleportYMock.Object);
            _teleportYMock.Setup(y => y.WithY(It.IsAny<int>())).Returns(_teleportOptionalMock.Object);
            _teleportOptionalMock.Setup(o => o.WithZ(It.IsAny<int>())).Returns(_teleportOptionalMock.Object);
            _teleportOptionalMock.Setup(o => o.WithDimension(It.IsAny<int>())).Returns(_teleportOptionalMock.Object);
            _teleportOptionalMock.Setup(o => o.Build()).Returns(_teleportMock.Object);
        }

        [TestMethod]
        public void Name_ShouldReturnCorrectName()
        {
            // Assert
            Assert.AreEqual("tele", _teleportCommand.Name);
        }

        [TestMethod]
        public void Permission_ShouldReturnCorrectPermission()
        {
            // Assert
            Assert.AreEqual(Permission.GameModerator, _teleportCommand.Permission);
        }

        [TestMethod]
        public async Task Execute_WithXY_ShouldTeleportCharacterToCorrectLocation()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var movementMock = new Mock<IMovement>();
            var location = new Location(10, 20, 0, 0);
            characterMock.Setup(c => c.Location).Returns(location);
            characterMock.Setup(c => c.Movement).Returns(movementMock.Object);

            var args = new GameCommandArgs(characterMock.Object, new[] { "123", "456" });

            // Act
            await _teleportCommand.Execute(args);

            // Assert
            _teleportTypeMock.Verify(t => t.WithX(123), Times.Once);
            _teleportYMock.Verify(y => y.WithY(456), Times.Once);
            _teleportOptionalMock.Verify(o => o.WithZ(0), Times.Once);
            _teleportOptionalMock.Verify(o => o.WithDimension(0), Times.Once);
            movementMock.Verify(m => m.Teleport(_teleportMock.Object), Times.Once);
        }

        [TestMethod]
        public async Task Execute_WithXYZ_ShouldTeleportCharacterToCorrectLocation()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var movementMock = new Mock<IMovement>();
            var location = new Location(10, 20, 0, 0);
            characterMock.Setup(c => c.Location).Returns(location);
            characterMock.Setup(c => c.Movement).Returns(movementMock.Object);

            var args = new GameCommandArgs(characterMock.Object, new[] { "123", "456", "7" });

            // Act
            await _teleportCommand.Execute(args);

            // Assert
            _teleportTypeMock.Verify(t => t.WithX(123), Times.Once);
            _teleportYMock.Verify(y => y.WithY(456), Times.Once);
            _teleportOptionalMock.Verify(o => o.WithZ(7), Times.Once);
            _teleportOptionalMock.Verify(o => o.WithDimension(0), Times.Once);
            movementMock.Verify(m => m.Teleport(_teleportMock.Object), Times.Once);
        }

        [TestMethod]
        public async Task Execute_WithXYZDim_ShouldTeleportCharacterToCorrectLocation()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var movementMock = new Mock<IMovement>();
            var location = new Location(10, 20, 0, 0);
            characterMock.Setup(c => c.Location).Returns(location);
            characterMock.Setup(c => c.Movement).Returns(movementMock.Object);

            var args = new GameCommandArgs(characterMock.Object, new[] { "123", "456", "7", "1" });

            // Act
            await _teleportCommand.Execute(args);

            // Assert
            _teleportTypeMock.Verify(t => t.WithX(123), Times.Once);
            _teleportYMock.Verify(y => y.WithY(456), Times.Once);
            _teleportOptionalMock.Verify(o => o.WithZ(7), Times.Once);
            _teleportOptionalMock.Verify(o => o.WithDimension(1), Times.Once);
            movementMock.Verify(m => m.Teleport(_teleportMock.Object), Times.Once);
        }
    }
}