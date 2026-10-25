using System;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Types.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;

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
            _cacheApiMock.Setup(x => x.GetFileId(5, "m0_123")).Returns(-1);
            _cacheApiMock.Setup(x => x.GetFileId(5, "l0_123")).Returns(1);
            var container = new Container(new MemoryStream());
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1, xteaKeys)).Returns(container);

            // Act
            _provider.Get(123, xteaKeys);

            // Assert
            _cacheApiMock.Verify(x => x.ReadContainer(5, 1, xteaKeys), Times.Once);
        }

        [Fact]
        public void Get_DisposesInputContainersAndCombinedStreamAfterDecode()
        {
            var terrainData = new MemoryStream(new byte[] { 81, 0, 0, 0, 0 });
            var objectData = new MemoryStream(new byte[] { 0x80, 0x00 });
            var terrainContainer = new Container(terrainData);
            var objectContainer = new Container(objectData);
            MemoryStream combinedStream = null!;
            var decodedMap = _typeFactory.CreateType(123);

            _cacheApiMock.Setup(x => x.GetFileId(5, "m0_123")).Returns(1);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(terrainContainer);
            _cacheApiMock.Setup(x => x.GetFileId(5, "l0_123")).Returns(2);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 2)).Returns(objectContainer);
            _codecMock.Setup(codec => codec.Decode(123, It.IsAny<MemoryStream>()))
                .Callback<int, MemoryStream>((_, stream) =>
                {
                    combinedStream = stream;
                    Assert.True(stream.CanRead);
                })
                .Returns(decodedMap);

            var actual = _provider.Get(123);

            Assert.Same(decodedMap, actual);
            Assert.Throws<ObjectDisposedException>(() => _ = terrainData.Length);
            Assert.Throws<ObjectDisposedException>(() => _ = objectData.Length);
            Assert.Throws<ObjectDisposedException>(() => _ = combinedStream.Length);
        }

        [Fact]
        public void Get_WithoutXteaKeys_ReadsObjectDataWithoutDecryption()
        {
            _cacheApiMock.Setup(x => x.GetFileId(5, "m0_123")).Returns(-1);
            _cacheApiMock.Setup(x => x.GetFileId(5, "l0_123")).Returns(1);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(new Container(new MemoryStream()));

            _provider.Get(123);

            _cacheApiMock.Verify(x => x.ReadContainer(5, 1), Times.Once);
            _cacheApiMock.Verify(x => x.ReadContainer(5, 1, It.IsAny<int[]>()), Times.Never);
        }

        [Fact]
        public void Get_WithEmptyXteaKeys_ReadsObjectDataWithoutDecryption()
        {
            _cacheApiMock.Setup(x => x.GetFileId(5, "m0_123")).Returns(-1);
            _cacheApiMock.Setup(x => x.GetFileId(5, "l0_123")).Returns(1);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(new Container(new MemoryStream()));

            _provider.Get(123, System.Array.Empty<int>());

            _cacheApiMock.Verify(x => x.ReadContainer(5, 1), Times.Once);
            _cacheApiMock.Verify(x => x.ReadContainer(5, 1, It.IsAny<int[]>()), Times.Never);
        }

        [Fact]
        public void DecodeRegion_WhenMapArchivesAreAbsent_UsesEmptyMapData()
        {
            _cacheApiMock.Setup(x => x.GetFileId(5, It.IsAny<string>())).Returns(-1);

            var decodedRegion = _provider.DecodeRegion(257, System.Array.Empty<int>(), CancellationToken.None);

            Assert.Empty(decodedRegion.ObjectPlacements);
            Assert.Empty(decodedRegion.BlockedTiles);
            _cacheApiMock.Verify(x => x.ReadContainer(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _cacheApiMock.Verify(x => x.ReadContainer(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()), Times.Never);
        }

        [Fact]
        public void DecodeRegion_WhenCancellationIsRequested_DoesNotReadMapArchives()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => _provider.DecodeRegion(
                257,
                System.Array.Empty<int>(),
                cancellation.Token));

            _cacheApiMock.Verify(x => x.GetFileId(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void DecodeRegion_WhenTerrainContainerCannotBeRead_PropagatesTheFailure()
        {
            AssertDecodeRegionPropagatesContainerFailure(terrainFileId: 1, objectFileId: -1);
        }

        [Fact]
        public void DecodeRegion_WhenObjectContainerCannotBeRead_PropagatesTheFailure()
        {
            AssertDecodeRegionPropagatesContainerFailure(terrainFileId: -1, objectFileId: 2);
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
            _cacheApiMock.Setup(x => x.ReadContainer(5, 2)).Returns(new Container(objectData));

            var objectDecoded = new Mock<ObjectDecoded>();
            var impassibleTerrainDecoded = new Mock<ImpassibleTerrainDecoded>();
            var calcRotation = new Mock<CalculateObjectPartRotation>();

            var request = new DecodePartRequest
            {
                RegionID = 257,
                XteaKeys = System.Array.Empty<int>(),
                MinX = 0,
                MinY = 0,
                MaxX = 63,
                MaxY = 63,
                PartZ = 0,
                PartRotation = 0,
                PartRotationCallback = calcRotation.Object,
                Callback = objectDecoded.Object,
                GroundCallback = impassibleTerrainDecoded.Object
            };

            // Act
            _provider.DecodePart(request);

            // Assert
            objectDecoded.Verify(x => x(0, 0, 1, It.IsAny<int>(), It.IsAny<int>(), 0), Times.Once);
            objectDecoded.Verify(x => x(0, 32, 0, It.IsAny<int>(), It.IsAny<int>(), 0), Times.Once);
        }

        [Fact]
        public void DecodeRegion_ReportsFullCoordinatesAcrossChunksAndPlanes()
        {
            // Arrange
            var terrainData = new MemoryStream(new byte[] { 0 });
            var objectData = CreateObjectData(
                (1, 24, 40, 0, 10, 2),
                (2, 9, 10, 2, 2, 3));

            _cacheApiMock.Setup(x => x.GetFileId(5, "m1_1")).Returns(1);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(new Container(terrainData));
            _cacheApiMock.Setup(x => x.GetFileId(5, "l1_1")).Returns(2);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 2)).Returns(new Container(objectData));

            // Act
            var decodedRegion = _provider.DecodeRegion(257, System.Array.Empty<int>(), CancellationToken.None);

            // Assert
            Assert.Collection(decodedRegion.ObjectPlacements,
                obj => Assert.Equal((1, 10, 2, 24, 40, 0), (obj.Id, obj.ShapeType, obj.Rotation, obj.X, obj.Y, obj.Z)),
                obj => Assert.Equal((2, 2, 3, 9, 10, 2), (obj.Id, obj.ShapeType, obj.Rotation, obj.X, obj.Y, obj.Z)));
            Assert.Empty(decodedRegion.BlockedTiles);
            Assert.Throws<ObjectDisposedException>(() => _ = terrainData.Length);
            Assert.Throws<ObjectDisposedException>(() => _ = objectData.Length);
        }

        [Fact]
        public void DecodeRegion_ReportsGroundFlagsOnEveryPlane()
        {
            // Arrange
            var terrainData = CreateTerrainData((0, 7, 8), (3, 9, 10));
            var objectData = new MemoryStream();

            _cacheApiMock.Setup(x => x.GetFileId(5, "m1_1")).Returns(1);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(new Container(terrainData));
            _cacheApiMock.Setup(x => x.GetFileId(5, "l1_1")).Returns(2);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 2)).Returns(new Container(objectData));

            // Act
            var decodedRegion = _provider.DecodeRegion(257, System.Array.Empty<int>(), CancellationToken.None);

            // Assert
            Assert.Equal([(7, 8, 0), (9, 10, 3)], decodedRegion.BlockedTiles.Select(tile => (tile.LocalX, tile.LocalY, tile.Z)));
            Assert.Empty(decodedRegion.ObjectPlacements);
        }

        [Fact]
        public void DecodePart_PreservesChunkLocalCoordinatesForPartRotation()
        {
            // Arrange
            var terrainData = new MemoryStream(new byte[] { 0 });
            var objectData = CreateObjectData((1, 9, 10, 0, 10, 0));

            _cacheApiMock.Setup(x => x.GetFileId(5, "m1_1")).Returns(1);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 1)).Returns(new Container(terrainData));
            _cacheApiMock.Setup(x => x.GetFileId(5, "l1_1")).Returns(2);
            _cacheApiMock.Setup(x => x.ReadContainer(5, 2)).Returns(new Container(objectData));

            var decodedObjects = new List<(int X, int Y)>();
            var request = new DecodePartRequest
            {
                RegionID = 257,
                XteaKeys = System.Array.Empty<int>(),
                MinX = 8,
                MinY = 8,
                MaxX = 15,
                MaxY = 15,
                PartZ = 0,
                PartRotation = 0,
                PartRotationCallback = (_, _, x, y, _, calculateRotationY) => calculateRotationY ? y : x,
                Callback = (_, _, _, x, y, _) => decodedObjects.Add((x, y)),
                GroundCallback = (_, _, _) => { }
            };

            // Act
            _provider.DecodePart(request);

            // Assert
            Assert.Equal([(9, 10)], decodedObjects);
        }

        private static MemoryStream CreateObjectData(params (int Id, int X, int Y, int Z, int ShapeType, int Rotation)[] placements)
        {
            var stream = new MemoryStream();
            var previousId = -1;
            foreach (var group in placements.GroupBy(placement => placement.Id).OrderBy(group => group.Key))
            {
                stream.WriteHugeSmart(group.Key - previousId);
                previousId = group.Key;

                var previousLocation = 0;
                foreach (var placement in group.OrderBy(placement => (placement.Z << 12) | (placement.X << 6) | placement.Y))
                {
                    var location = (placement.Z << 12) | (placement.X << 6) | placement.Y;
                    stream.WriteSmart(location - previousLocation + 1);
                    previousLocation = location;
                    stream.WriteByte((placement.ShapeType << 2) | placement.Rotation);
                }

                stream.WriteSmart(0);
            }

            stream.WriteHugeSmart(0);
            stream.Position = 0;
            return stream;
        }

        private static MemoryStream CreateTerrainData(params (int Z, int X, int Y)[] flaggedTiles)
        {
            var flagged = flaggedTiles.ToHashSet();
            var stream = new MemoryStream();
            for (var z = 0; z < 4; z++)
            for (var x = 0; x < 64; x++)
            for (var y = 0; y < 64; y++)
            {
                if (flagged.Contains((z, x, y)))
                {
                    stream.WriteByte(50);
                }

                stream.WriteByte(0);
            }

            stream.Position = 0;
            return stream;
        }

        private void AssertDecodeRegionPropagatesContainerFailure(int terrainFileId, int objectFileId)
        {
            var failure = new InvalidDataException("invalid map container");
            _cacheApiMock.Setup(x => x.GetFileId(5, "m1_1")).Returns(terrainFileId);
            _cacheApiMock.Setup(x => x.GetFileId(5, "l1_1")).Returns(objectFileId);
            var fileId = terrainFileId >= 0 ? terrainFileId : objectFileId;
            _cacheApiMock.Setup(x => x.ReadContainer(5, fileId)).Throws(failure);

            var actual = Assert.Throws<InvalidDataException>(() => _provider.DecodeRegion(
                257,
                System.Array.Empty<int>(),
                CancellationToken.None));

            Assert.Same(failure, actual);
        }
    }
}
