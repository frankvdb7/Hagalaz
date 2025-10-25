using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class MapDecoderTests
    {
        private readonly ICacheAPI _cacheApi;
        private readonly ILogger<MapDecoder> _logger;
        private readonly MapDecoder _mapDecoder;

        public MapDecoderTests()
        {
            _cacheApi = Substitute.For<ICacheAPI>();
            _logger = Substitute.For<ILogger<MapDecoder>>();
            _mapDecoder = new MapDecoder(_cacheApi, _logger);
        }

        [Fact]
        public void Decode_WithEmptyStreams_DoesNotThrow()
        {
            // Arrange
            var regionId = 12345;
            var xteaKeys = new int[4];
            _cacheApi.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(1);
            _cacheApi.ReadContainer(5, 1).Returns(new Container(new MemoryStream()));
            _cacheApi.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(2);
            _cacheApi.ReadContainer(5, 2, xteaKeys).Returns(new Container(new MemoryStream()));

            // Act & Assert
            var exception = Record.Exception(() => _mapDecoder.Decode(regionId, xteaKeys, (a,b,c,d,e,f) => {}, (a,b,c) => {}));
            Assert.Null(exception);
        }

        [Fact]
        public void Decode_WithValidTerrainData_CallsImpassableTerrainDecoded()
        {
            // Arrange
            var regionId = 12345;
            var xteaKeys = new int[4];
            var terrainData = new MemoryStream();

            // Tile (0,0,0): v=81 -> data=32 (not impassable)
            terrainData.WriteByte(81);
            terrainData.WriteByte(0);

            // Tile (0,0,1): v=50 -> data=1 (impassable)
            terrainData.WriteByte(50);
            terrainData.WriteByte(0);

            for (var i = 0; i < (4 * 64 * 64) - 2; i++)
            {
                terrainData.WriteByte(0);
            }
            terrainData.Position = 0;

            _cacheApi.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(1);
            _cacheApi.ReadContainer(5, 1).Returns(new Container(terrainData));
            _cacheApi.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(2);
            _cacheApi.ReadContainer(5, 2, xteaKeys).Returns(new Container(new MemoryStream()));

            var impassableTiles = new List<(int x, int y, int z)>();

            // Act
            _mapDecoder.Decode(regionId, xteaKeys, (a, b, c, d, e, f) => { }, (x, y, z) => impassableTiles.Add((x, y, z)));

            // Assert
            Assert.Single(impassableTiles);
            Assert.Equal((0, 1, 0), impassableTiles[0]);
        }

        [Fact]
        public void Decode_WithValidObjectData_CallsObjectDecoded()
        {
            // Arrange
            var regionId = 12345;
            var xteaKeys = new int[4];
            var objectData = new MemoryStream();
            objectData.WriteHugeSmart(10); // objectId = 9
            objectData.WriteSmart(1 + (10 << 6) + (3 << 12)); // location, localX=10, localY=0, z=3
            objectData.WriteByte((1 << 2) | 2); // shapeType=1, rotation=2
            objectData.WriteSmart(0);
            objectData.WriteHugeSmart(0);
            objectData.Position = 0;

            _cacheApi.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(1);
            _cacheApi.ReadContainer(5, 1).Returns(new Container(new MemoryStream()));
            _cacheApi.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(2);
            _cacheApi.ReadContainer(5, 2, xteaKeys).Returns(new Container(objectData));

            var decodedObjects = new List<(int objectId, int shapeType, int rotation, int x, int y, int z)>();

            // Act
            _mapDecoder.Decode(regionId, xteaKeys, (objectId, shapeType, rotation, x, y, z) => decodedObjects.Add((objectId, shapeType, rotation, x, y, z)), (a, b, c) => { });

            // Assert
            Assert.Single(decodedObjects);
            Assert.Equal((9, 1, 2, 10, 0, 3), decodedObjects[0]);
        }

        [Fact]
        public void ReadPartObjects_WithBoundingBox_OnlyDecodesObjectsWithinBounds()
        {
            // Arrange
            var regionId = 12345;
            var xteaKeys = new int[4];
            var objectData = new MemoryStream();

            // Object 1 (inside bounds)
            objectData.WriteHugeSmart(10); // objectId = 9
            int location1 = (5) + (5 << 6) + (1 << 12);
            objectData.WriteSmart(location1 + 1);
            objectData.WriteByte((1 << 2) | 2); // shapeType=1, rotation=2
            objectData.WriteSmart(0);

            // Object 2 (outside bounds)
            objectData.WriteHugeSmart(1); // objectId = 10
            int location2 = (15) + (15 << 6) + (1 << 12);
            objectData.WriteSmart(location2 - location1);
            objectData.WriteByte((2 << 2) | 3); // shapeType=2, rotation=3
            objectData.WriteSmart(0);

            objectData.WriteHugeSmart(0);
            objectData.Position = 0;

            _cacheApi.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(1);
            _cacheApi.ReadContainer(5, 1).Returns(new Container(new MemoryStream()));
            _cacheApi.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(2);
            _cacheApi.ReadContainer(5, 2, xteaKeys).Returns(new Container(objectData));

            var decodedObjects = new List<(int objectId, int shapeType, int rotation, int x, int y, int z)>();

            // Act
            _mapDecoder.ReadPartObjects(regionId, xteaKeys, 0, 0, 10, 10, 1, 0, DummyRotationCallback, (objectId, shapeType, rotation, x, y, z) => decodedObjects.Add((objectId, shapeType, rotation, x, y, z)), (a, b, c) => { });

            // Assert
            Assert.Single(decodedObjects);
            Assert.Equal(9, decodedObjects[0].objectId);
            Assert.Equal(5, decodedObjects[0].x);
            Assert.Equal(5, decodedObjects[0].y);
        }

        private int DummyRotationCallback(int objectId, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY)
        {
            if (calculateRotationY)
            {
                return yIndex;
            }
            return xIndex;
        }

        [Fact]
        public void ReadPartObjects_WithRotation_CorrectlyRotatesObjects()
        {
            // Arrange
            var regionId = 12345;
            var xteaKeys = new int[4];
            var objectData = new MemoryStream();

            objectData.WriteHugeSmart(10); // objectId = 9
            int location = (5) + (5 << 6) + (1 << 12);
            objectData.WriteSmart(location + 1);
            objectData.WriteByte((1 << 2) | 2); // shapeType=1, rotation=2
            objectData.WriteSmart(0);
            objectData.WriteHugeSmart(0);
            objectData.Position = 0;

            _cacheApi.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(1);
            _cacheApi.ReadContainer(5, 1).Returns(new Container(new MemoryStream()));
            _cacheApi.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF)).Returns(2);
            _cacheApi.ReadContainer(5, 2, xteaKeys).Returns(new Container(objectData));

            var decodedObjects = new List<(int objectId, int shapeType, int rotation, int x, int y, int z)>();

            // Act
            _mapDecoder.ReadPartObjects(regionId, xteaKeys, 0, 0, 10, 10, 1, 1, RotatingRotationCallback, (objectId, shapeType, rotation, x, y, z) => decodedObjects.Add((objectId, shapeType, rotation, x, y, z)), (a, b, c) => { });

            // Assert
            Assert.Single(decodedObjects);
            Assert.Equal(9, decodedObjects[0].objectId);
            Assert.Equal(2, decodedObjects[0].x);
            Assert.Equal(5, decodedObjects[0].y);
        }

        private int RotatingRotationCallback(int objectId, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY)
        {
            if (calculateRotationY)
            {
                return yIndex;
            }
            return 7 - yIndex;
        }
    }
}
