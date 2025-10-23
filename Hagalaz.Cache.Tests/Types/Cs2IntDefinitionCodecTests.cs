using System.IO;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class Cs2IntDefinitionCodecTests
    {
        [Fact]
        public void RoundTripTest()
        {
            // Arrange
            var codec = new Cs2IntDefinitionCodec();
            var original = new Cs2IntDefinition(1)
            {
                AChar327 = 'A',
                AnInt325 = 0
            };

            // Act
            var encodedStream = codec.Encode(original);
            var decoded = codec.Decode(1, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.AChar327, decoded.AChar327);
            Assert.Equal(original.AnInt325, decoded.AnInt325);
        }
    }
}
