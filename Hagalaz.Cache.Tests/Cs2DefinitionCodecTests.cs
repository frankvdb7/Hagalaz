using System.Collections.Generic;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class Cs2DefinitionCodecTests
    {
        [Fact]
        public void RoundTrip_ValidDefinition_DecodesCorrectly()
        {
            // Arrange
            var codec = new Cs2DefinitionCodec();
            var original = new Cs2Definition(1)
            {
                Name = "Test Script",
                IntLocalsCount = 1,
                StringLocalsCount = 1,
                LongLocalsCount = 1,
                IntArgsCount = 1,
                StringArgsCount = 1,
                LongArgsCount = 1,
                Switches = new IReadOnlyDictionary<int, int>[]
                {
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 } },
                    new Dictionary<int, int> { { 3, 30 } }
                },
                Opcodes = new[] { 3, 63, 145 },
                IntPool = new[] { 123, 0, 0 },
                StringPool = new[] { "", "", "test" },
                LongPool = new[] { 0L, 456L, 0L }
            };

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = codec.Decode(1, encodedStream);

            // Assert
            Assert.Equal(original.Name, decoded.Name);
            Assert.Equal(original.IntLocalsCount, decoded.IntLocalsCount);
            Assert.Equal(original.StringLocalsCount, decoded.StringLocalsCount);
            Assert.Equal(original.LongLocalsCount, decoded.LongLocalsCount);
            Assert.Equal(original.IntArgsCount, decoded.IntArgsCount);
            Assert.Equal(original.StringArgsCount, decoded.StringArgsCount);
            Assert.Equal(original.LongArgsCount, decoded.LongArgsCount);
            Assert.Equal(original.Switches.Length, decoded.Switches.Length);
            for (int i = 0; i < original.Switches.Length; i++)
            {
                Assert.Equal(original.Switches[i], decoded.Switches[i]);
            }
            Assert.Equal(original.Opcodes, decoded.Opcodes);
            Assert.Equal(original.IntPool, decoded.IntPool);
            Assert.Equal(original.StringPool, decoded.StringPool);
            Assert.Equal(original.LongPool, decoded.LongPool);
        }
    }
}
