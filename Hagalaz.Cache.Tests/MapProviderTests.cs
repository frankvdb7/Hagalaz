using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Providers;
using Hagalaz.Cache.Types.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.IO;

namespace Hagalaz.Cache.Tests
{
    public class MapProviderTests
    {
        private readonly Mock<ICacheAPI> _cacheApiMock;
        private readonly Mock<IMapCodec> _codecMock;
        private readonly MapTypeFactory _typeFactory;
        private readonly Mock<ILogger<MapProvider>> _loggerMock;
        private readonly MapProvider _provider;

        public MapProviderTests()
        {
            _cacheApiMock = new Mock<ICacheAPI>();
            _codecMock = new Mock<IMapCodec>();
            _typeFactory = new MapTypeFactory();
            _loggerMock = new Mock<ILogger<MapProvider>>();
            _provider = new MapProvider(_cacheApiMock.Object, _codecMock.Object, _typeFactory, _loggerMock.Object);
        }

        [Fact]
        public void Get_WithXteaKeys_PassesKeysToCacheApi()
        {
            // Arrange
            var xteaKeys = new[] { 1, 2, 3, 4 };
            _cacheApiMock.Setup(x => x.GetFileId(5, It.IsAny<string>())).Returns(1);
            var container = new Container(new MemoryStream());
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1, xteaKeys)).Returns(container);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(container);

            // Act
            _provider.Get(123, xteaKeys);

            // Assert
            _cacheApiMock.Verify(x => x.ReadContainer(5, 1, xteaKeys), Times.Once);
        }

        [Fact]
        public void DecodePart_ValidData_InvokesCallbacks()
        {
            // Arrange
            var terrainData = new MemoryStream(new byte[] { 81, 0, 0, 0, 0 });
            var objectData = new MemoryStream(new byte[] { 0x80, 0x01, 0x40, 0x01, 0x1C, 0x80, 0x00 });

            _cacheApiMock.Setup(x => x.GetFileId(5, "m1_1")).Returns(1);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(new Container(terrainData));
            _cacheApiMock.Setup(x => x.GetFileId(5, "l1_1")).Returns(2);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 2, It.IsAny<int[]>())).Returns(new Container(objectData));

            var objectDecoded = new Mock<ObjectDecoded>();
            var impassibleTerrainDecoded = new Mock<ImpassibleTerrainDecoded>();
            var calcRotation = new Mock<CalculateObjectPartRotation>();

            // Act
            _provider.DecodePart(257, System.Array.Empty<int>(), 0, 0, 63, 63, 0, 0,
                calcRotation.Object, objectDecoded.Object, impassibleTerrainDecoded.Object);

            // Assert
            objectDecoded.Verify(x => x(1, 7, 0, It.IsAny<int>(), It.IsAny<int>(), 0), Times.Once);
        }
    }
}
