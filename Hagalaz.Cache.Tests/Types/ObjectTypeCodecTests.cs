using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Moq;
using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class ObjectTypeCodecTests
    {
        [Fact]
        public void DecodeAndEncode_Symmetric()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var objectType = new ObjectType(1)
            {
                Name = "Test Object",
                SizeX = 2,
                SizeY = 3,
                Solid = true,
                Interactable = 1,
                ClipType = 1,
                Actions = new[] { "Examine", null, null, null, null }
            };

            // Act
            var encodedStream = codec.Encode(objectType);
            encodedStream.Position = 0;
            var decodedObjectType = codec.Decode(1, encodedStream);

            // Assert
            Assert.Equal(objectType.Name, decodedObjectType.Name);
            Assert.Equal(objectType.SizeX, decodedObjectType.SizeX);
            Assert.Equal(objectType.SizeY, decodedObjectType.SizeY);
            Assert.Equal(objectType.Solid, decodedObjectType.Solid);
            Assert.Equal(objectType.Interactable, ((ObjectType)decodedObjectType).Interactable);
            Assert.Equal(objectType.ClipType, decodedObjectType.ClipType);
            Assert.Equal(objectType.Actions[0], decodedObjectType.Actions[0]);
        }
    }
}