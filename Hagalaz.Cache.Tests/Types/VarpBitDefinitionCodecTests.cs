using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class VarpBitDefinitionCodecTests
    {
        [Fact]
        public void RoundTrip_WithValidData_ShouldSucceed()
        {
            // Arrange
            var codec = new VarpBitDefinitionCodec();
            var original = new VarpBitDefinition(1)
            {
                ConfigID = 123,
                BitOffset = 4,
                BitLength = 8
            };

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.ConfigID, decoded.ConfigID);
            Assert.Equal(original.BitOffset, decoded.BitOffset);
            Assert.Equal(original.BitLength, decoded.BitLength);
        }
    }
}
