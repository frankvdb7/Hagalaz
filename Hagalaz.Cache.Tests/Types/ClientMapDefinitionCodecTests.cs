using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using System.Collections.Generic;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class ClientMapDefinitionCodecTests
    {
        [Fact]
        public void TestRoundTrip_WithValueMap_String()
        {
            // Arrange
            var codec = new ClientMapDefinitionCodec();
            var original = new ClientMapDefinition(1)
            {
                KeyType = 'i',
                ValType = 's',
                DefaultStringValue = "test",
                DefaultIntValue = 123,
                Count = 2,
                ValueMap = new Dictionary<int, object>
                {
                    { 1, "one" },
                    { 2, "two" }
                }
            };

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.KeyType, decoded.KeyType);
            Assert.Equal(original.ValType, decoded.ValType);
            Assert.Equal(original.DefaultStringValue, decoded.DefaultStringValue);
            Assert.Equal(original.DefaultIntValue, decoded.DefaultIntValue);
            Assert.Equal(original.Count, decoded.Count);
            Assert.Equal(original.ValueMap, decoded.ValueMap);
        }

        [Fact]
        public void TestRoundTrip_WithValueMap_Int()
        {
            // Arrange
            var codec = new ClientMapDefinitionCodec();
            var original = new ClientMapDefinition(1)
            {
                KeyType = 'i',
                ValType = 'i',
                DefaultStringValue = "test",
                DefaultIntValue = 123,
                Count = 2,
                ValueMap = new Dictionary<int, object>
                {
                    { 1, 11 },
                    { 2, 22 }
                }
            };

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.KeyType, decoded.KeyType);
            Assert.Equal(original.ValType, decoded.ValType);
            Assert.Equal(original.DefaultStringValue, decoded.DefaultStringValue);
            Assert.Equal(original.DefaultIntValue, decoded.DefaultIntValue);
            Assert.Equal(original.Count, decoded.Count);
            Assert.Equal(original.ValueMap, decoded.ValueMap);
        }

        [Fact]
        public void TestRoundTrip_WithValues_String()
        {
            // Arrange
            var codec = new ClientMapDefinitionCodec();
            var original = new ClientMapDefinition(1)
            {
                KeyType = 'i',
                ValType = 's',
                DefaultStringValue = "test",
                DefaultIntValue = 123,
                Count = 2,
                Values = new object[5]
            };
            original.Values[1] = "one";
            original.Values[3] = "three";


            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.KeyType, decoded.KeyType);
            Assert.Equal(original.ValType, decoded.ValType);
            Assert.Equal(original.DefaultStringValue, decoded.DefaultStringValue);
            Assert.Equal(original.DefaultIntValue, decoded.DefaultIntValue);
            Assert.Equal(original.Count, decoded.Count);
            Assert.Equal(original.Values, decoded.Values);
        }

        [Fact]
        public void TestRoundTrip_WithValues_Int()
        {
            // Arrange
            var codec = new ClientMapDefinitionCodec();
            var original = new ClientMapDefinition(1)
            {
                KeyType = 'i',
                ValType = 'i',
                DefaultStringValue = "test",
                DefaultIntValue = 123,
                Count = 2,
                Values = new object[5]
            };
            original.Values[1] = 11;
            original.Values[3] = 33;


            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.KeyType, decoded.KeyType);
            Assert.Equal(original.ValType, decoded.ValType);
            Assert.Equal(original.DefaultStringValue, decoded.DefaultStringValue);
            Assert.Equal(original.DefaultIntValue, decoded.DefaultIntValue);
            Assert.Equal(original.Count, decoded.Count);
            Assert.Equal(original.Values, decoded.Values);
        }
    }
}
