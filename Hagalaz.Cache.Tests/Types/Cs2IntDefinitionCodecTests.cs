using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class Cs2IntDefinitionCodecTests
    {
        public static IEnumerable<object[]> RoundTripTestData =>
            new List<object[]>
            {
                new object[] { new Cs2IntDefinition(1) { AChar327 = 'A', AnInt325 = 0 } },
                new object[] { new Cs2IntDefinition(2) }, // Default values
                new object[] { new Cs2IntDefinition(3) { AChar327 = 'B' } }, // Only AChar327 set
                new object[] { new Cs2IntDefinition(4) { AnInt325 = 0 } }, // Only AnInt325 set
            };

        [Theory]
        [MemberData(nameof(RoundTripTestData))]
        public void RoundTripTest(Cs2IntDefinition original)
        {
            // Arrange
            var codec = new Cs2IntDefinitionCodec();

            // Act
            var encodedStream = codec.Encode(original);
            var decoded = codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.AChar327, decoded.AChar327);
            Assert.Equal(original.AnInt325, decoded.AnInt325);
        }
    }
}
