using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ItemTypeCodecTests
    {
        [Fact]
        public void Encode_Decode_RoundTrip_ShouldRestoreSameData()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var originalItem = new ItemType(123)
            {
                Name = "Test Item",
                Value = 1000,
                MembersOnly = true,
                StackableType = 1,
                ModelZoom = 2500,
                InterfaceModelId = 54321,
                ExtraData = new Dictionary<int, object>
                {
                    { 1, "string value" },
                    { 2, 12345 }
                }
            };

            // Act
            var encodedStream = codec.Encode(originalItem);
            encodedStream.Position = 0;
            var decodedItem = (ItemType)codec.Decode(originalItem.Id, encodedStream);

            // Assert
            Assert.Equal(originalItem.Id, decodedItem.Id);
            Assert.Equal(originalItem.Name, decodedItem.Name);
            Assert.Equal(originalItem.Value, decodedItem.Value);
            Assert.Equal(originalItem.MembersOnly, decodedItem.MembersOnly);
            Assert.Equal(originalItem.StackableType, decodedItem.StackableType);
            Assert.Equal(originalItem.ModelZoom, decodedItem.ModelZoom);
            Assert.Equal(originalItem.InterfaceModelId, decodedItem.InterfaceModelId);
            Assert.Equal(originalItem.ExtraData, decodedItem.ExtraData);
        }

        [Fact]
        public void Decode_WithEmptyStream_ShouldReturnDefaultItem()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(0); // End of data
            stream.Position = 0;
            var defaultItem = new ItemType(456);

            // Act
            var decodedItem = (ItemType)codec.Decode(456, stream);

            // Assert
            Assert.Equal(defaultItem.Id, decodedItem.Id);
            Assert.Equal(defaultItem.Name, decodedItem.Name);
            Assert.Equal(defaultItem.Value, decodedItem.Value);
        }
    }
}