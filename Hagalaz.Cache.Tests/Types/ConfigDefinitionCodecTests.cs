using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class ConfigDefinitionCodecTests
    {
        [Theory]
        [InlineData(0, '\0')]
        [InlineData(ushort.MaxValue, '\x7f')]
        public void RoundTripTest(int defaultValue, char valueType)
        {
            // Arrange
            var codec = new ConfigDefinitionCodec();
            var original = new ConfigDefinition(1)
            {
                ValueType = valueType,
                DefaultValue = defaultValue
            };

            // Act
            using var stream = codec.Encode(original);
            stream.Position = 0;
            var decoded = codec.Decode(1, stream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.ValueType, decoded.ValueType);
            Assert.Equal(original.DefaultValue, decoded.DefaultValue);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Encode_RejectsDefaultValuesOutsideUnsignedShortRange(int defaultValue)
        {
            var codec = new ConfigDefinitionCodec();
            var definition = new ConfigDefinition(1) { DefaultValue = defaultValue };

            Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(definition));
        }

        [Theory]
        [InlineData('\x80')]
        [InlineData('\u0100')]
        public void Encode_RejectsValueTypesOutsideAsciiRange(char valueType)
        {
            var codec = new ConfigDefinitionCodec();
            var definition = new ConfigDefinition(1) { ValueType = valueType };

            Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(definition));
        }
    }
}
