using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
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
    }
}
