using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class Cs2DefinitionCodecTests
    {
        // Instruction widths and footer layout follow ClientScript.java and CS2InstructionInfo.java.
        private const string Revision742Script =
            "4100" + "0000FF" + "0009FFFFFFFE" + "00918000" + "003F8000000000000001" +
            "00000004000100020003000400050006" + "010001FFFFFFFF00000002" + "000B";

        [Fact]
        public void Decode_Revision742Script_PreservesInstructionsAndSwitchOffsets()
        {
            var payload = Convert.FromHexString(Revision742Script);
            var codec = new Cs2DefinitionCodec();
            using var stream = new MemoryStream(payload);
            var script = codec.Decode(42, stream);

            Assert.Equal("A", script.Name);
            Assert.Equal(new[] { 0, 9, 145, 63 }, script.Opcodes);
            Assert.Equal(new[] { 255, -2, 0, 0 }, script.IntPool);
            Assert.Equal("€", script.StringPool[2]);
            Assert.Equal(long.MinValue + 1, script.LongPool[3]);
            Assert.Equal(new KeyValuePair<int, int>(-1, 2), Assert.Single(Assert.Single(script.Switches)));
            using var encoded = codec.Encode(script);
            Assert.Equal(payload, encoded.ToArray());
        }

        [Fact]
        public void Decode_InstructionCountDoesNotMatchFooter_RejectsScript()
        {
            var payload = Convert.FromHexString(Revision742Script);
            payload[26] = 3;
            using var stream = new MemoryStream(payload);

            Assert.Throws<InvalidDataException>(() => new Cs2DefinitionCodec().Decode(42, stream));
        }

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
