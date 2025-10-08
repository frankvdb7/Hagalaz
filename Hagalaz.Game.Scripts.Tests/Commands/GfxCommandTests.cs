using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class GfxCommandTests
    {
        private Mock<IRegionUpdateBuilder> _regionUpdateBuilderMock;
        private Mock<IRegionUpdateLocation> _regionUpdateLocationMock;
        private Mock<IRegionUpdateOptional> _regionUpdateOptionalMock;
        private Mock<IRegionUpdateBuild> _regionUpdateBuildMock;
        private Mock<IRegionPartUpdate> _regionPartUpdateMock;
        private GfxCommand _gfxCommand;

        [TestInitialize]
        public void Setup()
        {
            _regionUpdateBuilderMock = new Mock<IRegionUpdateBuilder>();
            _regionUpdateLocationMock = new Mock<IRegionUpdateLocation>();
            _regionUpdateOptionalMock = new Mock<IRegionUpdateOptional>();
            _regionUpdateBuildMock = new Mock<IRegionUpdateBuild>();
            _regionPartUpdateMock = new Mock<IRegionPartUpdate>();

            _gfxCommand = new GfxCommand(_regionUpdateBuilderMock.Object);

            _regionUpdateBuilderMock.Setup(b => b.Create()).Returns(_regionUpdateLocationMock.Object);
            _regionUpdateLocationMock.Setup(l => l.WithLocation(It.IsAny<ILocation>())).Returns(_regionUpdateOptionalMock.Object);
            _regionUpdateOptionalMock.Setup(o => o.WithGraphic(It.IsAny<IGraphic>())).Returns(_regionUpdateBuildMock.Object);
            _regionUpdateBuildMock.Setup(b => b.Build()).Returns(_regionPartUpdateMock.Object);
        }

        [TestMethod]
        public void Name_ShouldReturnCorrectName()
        {
            // Assert
            Assert.AreEqual("gfx", _gfxCommand.Name);
        }

        [TestMethod]
        public void Permission_ShouldReturnCorrectPermission()
        {
            // Assert
            Assert.AreEqual(Permission.SystemAdministrator, _gfxCommand.Permission);
        }

        [TestMethod]
        public async Task Execute_WithId_ShouldQueueUpdateWithCorrectGfx()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var regionMock = new Mock<IMapRegion>();
            var location = new Location(10, 20, 0, 0);
            characterMock.Setup(c => c.Location).Returns(location);
            characterMock.Setup(c => c.Region).Returns(regionMock.Object);

            var args = new GameCommandArgs(characterMock.Object, new[] { "gfx", "123" });

            // Act
            await _gfxCommand.Execute(args);

            // Assert
            _regionUpdateOptionalMock.Verify(o => o.WithGraphic(It.Is<IGraphic>(g => g.Id == 123 && g.Delay == 0 && g.Height == 0)), Times.Once);
            regionMock.Verify(r => r.QueueUpdate(_regionPartUpdateMock.Object), Times.Once);
        }

        [TestMethod]
        public async Task Execute_WithIdAndHeight_ShouldQueueUpdateWithCorrectGfx()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var regionMock = new Mock<IMapRegion>();
            var location = new Location(10, 20, 0, 0);
            characterMock.Setup(c => c.Location).Returns(location);
            characterMock.Setup(c => c.Region).Returns(regionMock.Object);

            var args = new GameCommandArgs(characterMock.Object, new[] { "gfx", "123", "50" });

            // Act
            await _gfxCommand.Execute(args);

            // Assert
            _regionUpdateOptionalMock.Verify(o => o.WithGraphic(It.Is<IGraphic>(g => g.Id == 123 && g.Delay == 0 && g.Height == 50)), Times.Once);
            regionMock.Verify(r => r.QueueUpdate(_regionPartUpdateMock.Object), Times.Once);
        }

        [TestMethod]
        public async Task Execute_WithIdHeightAndRotation_ShouldQueueUpdateWithCorrectGfx()
        {
            // Arrange
            var characterMock = new Mock<ICharacter>();
            var regionMock = new Mock<IMapRegion>();
            var location = new Location(10, 20, 0, 0);
            characterMock.Setup(c => c.Location).Returns(location);
            characterMock.Setup(c => c.Region).Returns(regionMock.Object);

            var args = new GameCommandArgs(characterMock.Object, new[] { "gfx", "123", "50", "2" });

            // Act
            await _gfxCommand.Execute(args);

            // Assert
            _regionUpdateOptionalMock.Verify(o => o.WithGraphic(It.Is<IGraphic>(g => g.Id == 123 && g.Delay == 0 && g.Height == 50 && g.Rotation == 2)), Times.Once);
            regionMock.Verify(r => r.QueueUpdate(_regionPartUpdateMock.Object), Times.Once);
        }
    }
}