using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class MapCodecTests
    {
        [Fact]
        public void RoundTrip_WithComplexData_ShouldSucceed()
        {
            // Arrange
            var codec = new MapCodec();
            var originalMap = new MapType { Id = 123 };
            originalMap.TerrainData[0, 10, 20] = 5;
            originalMap.InternalObjects.Add(new MapObject { Id = 1, X = 10, Y = 20, Z = 0, ShapeType = 7, Rotation = 1 });
            originalMap.InternalObjects.Add(new MapObject { Id = 1, X = 11, Y = 21, Z = 0, ShapeType = 7, Rotation = 1 });
            originalMap.InternalObjects.Add(new MapObject { Id = 5, X = 30, Y = 40, Z = 1, ShapeType = 8, Rotation = 2 });

            // Act
            var encodedStream = codec.Encode(originalMap);
            var decodedMap = codec.Decode(123, encodedStream);

            // Assert
            Assert.Equal(originalMap.Id, decodedMap.Id);
            Assert.Equal(originalMap.TerrainData, decodedMap.TerrainData);
            Assert.Equal(originalMap.Objects.Count, decodedMap.Objects.Count);
            for (var i = 0; i < originalMap.Objects.Count; i++)
            {
                var expected = originalMap.Objects[i];
                var actual = decodedMap.Objects[i];
                Assert.Equal(expected.Id, actual.Id);
                Assert.Equal(expected.X, actual.X);
                Assert.Equal(expected.Y, actual.Y);
                Assert.Equal(expected.Z, actual.Z);
                Assert.Equal(expected.ShapeType, actual.ShapeType);
                Assert.Equal(expected.Rotation, actual.Rotation);
            }
        }

        [Fact]
        public void DecodeTerrainData_ValidStream_DecodesCorrectly()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 81, 0, 0, 0, 0 });
            var terrainData = new sbyte[4, 64, 64];

            // Act
            MapCodec.DecodeTerrainData(terrainData, stream);

            // Assert
            Assert.Equal(32, terrainData[0, 0, 0]);
        }

        [Fact]
        public void DecodeObjectData_ValidStream_DecodesCorrectly()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 0x80, 0x01, 0x40, 0x01, 0x1C, 0x80, 0x00 });
            var objects = new List<MapObject>();
            var terrainData = new sbyte[4, 64, 64];

            // Act
            MapCodec.DecodeObjectData(objects, terrainData, stream);

            // Assert
            Assert.Collection(objects,
                obj1 =>
                {
                    Assert.Equal(0, obj1.Id);
                    Assert.Equal(0, obj1.X);
                    Assert.Equal(63, obj1.Y);
                    Assert.Equal(0, obj1.Z);
                    Assert.Equal(0, obj1.ShapeType);
                    Assert.Equal(1, obj1.Rotation);
                },
                obj2 =>
                {
                    Assert.Equal(0, obj2.Id);
                    Assert.Equal(1, obj2.X);
                    Assert.Equal(26, obj2.Y);
                    Assert.Equal(0, obj2.Z);
                    Assert.Equal(32, obj2.ShapeType);
                    Assert.Equal(0, obj2.Rotation);
                });
        }
    }
}
