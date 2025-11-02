using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class ConfigDefinitionCodecTests
    {
        [Fact]
        public void RoundTripTest()
        {
            // Arrange
            var codec = new ConfigDefinitionCodec();
            var original = new ConfigDefinition(1)
            {
                ValueType = 'A',
                DefaultValue = 123
            };

            // Act
            var stream = codec.Encode(original);
            stream.Position = 0;
            var decoded = codec.Decode(1, stream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.ValueType, decoded.ValueType);
            Assert.Equal(original.DefaultValue, decoded.DefaultValue);
        }
    }
}
