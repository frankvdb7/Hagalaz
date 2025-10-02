using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ObjectTypeCodecTests
    {
        [Fact]
        public void Encode_Decode_RoundTrip_ShouldRestoreSameData()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var originalObject = new ObjectType(123)
            {
                Name = "Test Object",
                SizeX = 2,
                SizeY = 3,
                Solid = false,
                Interactable = 1,
                Actions = new[] { "Action1", "Action2", null, null, null, "Examine" },
                ExtraData = new Dictionary<int, object>
                {
                    { 1, "string value" },
                    { 2, 12345 }
                }
            };

            // Act
            var encodedStream = codec.Encode(originalObject);
            encodedStream.Position = 0;
            var decodedObject = (ObjectType)codec.Decode(originalObject.Id, encodedStream);

            // Assert
            Assert.Equal(originalObject.Id, decodedObject.Id);
            Assert.Equal(originalObject.Name, decodedObject.Name);
            Assert.Equal(originalObject.SizeX, decodedObject.SizeX);
            Assert.Equal(originalObject.SizeY, decodedObject.SizeY);
            Assert.Equal(originalObject.Solid, decodedObject.Solid);
            Assert.Equal(originalObject.Interactable, decodedObject.Interactable);
            Assert.Equal(originalObject.Actions, decodedObject.Actions);
            Assert.Equal(originalObject.ExtraData, decodedObject.ExtraData);
        }

        [Fact]
        public void Decode_WithEmptyStream_ShouldReturnDefaultObject()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(0); // End of data
            stream.Position = 0;
            var defaultObject = new ObjectType(456);

            // Act
            var decodedObject = (ObjectType)codec.Decode(456, stream);

            // Assert
            Assert.Equal(defaultObject.Id, decodedObject.Id);
            Assert.Equal(defaultObject.Name, decodedObject.Name);
            Assert.Equal(defaultObject.SizeX, decodedObject.SizeX);
        }
    }
}