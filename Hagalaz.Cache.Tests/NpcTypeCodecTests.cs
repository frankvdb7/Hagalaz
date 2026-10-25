using Hagalaz.Cache.Types;
using Hagalaz.Cache.Logic.Codecs;
using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class NpcTypeCodecTests
    {
        [Fact]
        public void TestEncodeDecode_AllProperties()
        {
            // Arrange
            var codec = new NpcTypeCodec();
            var originalNpc = new NpcType(1)
            {
                Name = "Test NPC",
                CombatLevel = 100,
                IsVisible = true,
                Actions = new string?[] { "Talk-to", "Attack", "Trade", null, null, "Examine" },
                Size = 2,
                ModelIDs = new[] { 10, 20, 30 },
                OriginalColours = new short[] { 100, 200 },
                ModifiedColours = new short[] { 101, 201 },
                ExtraData = new Dictionary<int, object>
                {
                    { 1, "test_string" },
                    { 2, 12345 }
                },
                VisibleOnMinimap = false,
                ScaleX = 150,
                ScaleY = 150,
                IsClickable = false,
                SpawnFaceDirection = 3,
                RenderId = 99,
                TransformToIDs = new []{ 1, 2, 3, -1}
            };

            // Act
            var encodedStream = codec.Encode(originalNpc);
            encodedStream.Position = 0;
            var decodedNpc = (NpcType)codec.Decode(1, encodedStream);

            // Assert
            Assert.Equal(originalNpc.Name, decodedNpc.Name);
            Assert.Equal(originalNpc.CombatLevel, decodedNpc.CombatLevel);
            Assert.Equal(originalNpc.IsVisible, decodedNpc.IsVisible);
            Assert.Equal(originalNpc.Actions, decodedNpc.Actions);
            Assert.Equal(originalNpc.Size, decodedNpc.Size);
            Assert.Equal(originalNpc.ModelIDs, decodedNpc.ModelIDs);
            Assert.Equal(originalNpc.OriginalColours, decodedNpc.OriginalColours);
            Assert.Equal(originalNpc.ModifiedColours, decodedNpc.ModifiedColours);
            Assert.Equal(originalNpc.ExtraData, decodedNpc.ExtraData);
            Assert.Equal(originalNpc.VisibleOnMinimap, decodedNpc.VisibleOnMinimap);
            Assert.Equal(originalNpc.ScaleX, decodedNpc.ScaleX);
            Assert.Equal(originalNpc.ScaleY, decodedNpc.ScaleY);
            Assert.Equal(originalNpc.IsClickable, decodedNpc.IsClickable);
            Assert.Equal(originalNpc.SpawnFaceDirection, decodedNpc.SpawnFaceDirection);
            Assert.Equal(originalNpc.RenderId, decodedNpc.RenderId);
            Assert.Equal(originalNpc.TransformToIDs, decodedNpc.TransformToIDs);
        }

        [Fact]
        public void TestEncodeDecode_DefaultValues()
        {
            // Arrange
            var codec = new NpcTypeCodec();
            var originalNpc = new NpcType(2);

            // Act
            var encodedStream = codec.Encode(originalNpc);
            encodedStream.Position = 0;
            var decodedNpc = (NpcType)codec.Decode(2, encodedStream);

            // Assert
            Assert.Equal(originalNpc.Name, decodedNpc.Name);
            Assert.Equal(originalNpc.CombatLevel, decodedNpc.CombatLevel);
            Assert.Equal(originalNpc.IsVisible, decodedNpc.IsVisible);
            Assert.Equal(originalNpc.Actions, decodedNpc.Actions);
            Assert.Equal(originalNpc.Size, decodedNpc.Size);
            Assert.Null(decodedNpc.ModelIDs);
            Assert.Null(decodedNpc.OriginalColours);
            Assert.Null(decodedNpc.ModifiedColours);
            Assert.Null(decodedNpc.ExtraData);
        }

        [Fact]
        public void TestEncodeDecode_EmptyCollections()
        {
            // Arrange
            var codec = new NpcTypeCodec();
            var originalNpc = new NpcType(3)
            {
                Name = "Empty NPC",
                ModelIDs = System.Array.Empty<int>(),
                OriginalColours = System.Array.Empty<short>(),
                ModifiedColours = System.Array.Empty<short>(),
                ExtraData = new Dictionary<int, object>(),
                TransformToIDs = System.Array.Empty<int>()
            };

            // Act
            var encodedStream = codec.Encode(originalNpc);
            encodedStream.Position = 0;
            var decodedNpc = (NpcType)codec.Decode(3, encodedStream);

            // Assert
            Assert.Equal(originalNpc.Name, decodedNpc.Name);
            Assert.Equal(originalNpc.ModelIDs, decodedNpc.ModelIDs);
            Assert.Null(decodedNpc.OriginalColours);
            Assert.Null(decodedNpc.ModifiedColours);
            Assert.Equal(originalNpc.ExtraData, decodedNpc.ExtraData);
            Assert.Null(decodedNpc.TransformToIDs);
        }

        [Fact]
        public void TestEncodeDecode_TransformToIDs_Empty()
        {
            // Arrange
            var codec = new NpcTypeCodec();
            var originalNpc = new NpcType(4)
            {
                Name = "TransformToIDs Empty",
                TransformToIDs = System.Array.Empty<int>()
            };

            // Act
            var encodedStream = codec.Encode(originalNpc);
            encodedStream.Position = 0;
            var decodedNpc = (NpcType)codec.Decode(4, encodedStream);

            // Assert
            Assert.Null(decodedNpc.TransformToIDs);
        }

        [Fact]
        public void TestEncodeDecode_TransformToIDs_SingleElement()
        {
            // Arrange
            var codec = new NpcTypeCodec();
            var originalNpc = new NpcType(5)
            {
                Name = "TransformToIDs Single Element",
                TransformToIDs = new[] { 1 }
            };

            // Act
            var encodedStream = codec.Encode(originalNpc);
            encodedStream.Position = 0;
            var decodedNpc = (NpcType)codec.Decode(5, encodedStream);

            // Assert
            Assert.Null(decodedNpc.TransformToIDs);
        }
        [Fact]
        public void Encode_WithNullActions_ShouldNotThrow()
        {
            // Arrange
            var codec = new NpcTypeCodec();
            var npc = new NpcType(10)
            {
                Actions = new string?[] { null, "Attack", null, "Trade", null }
            };

            // Act & Assert
            codec.Encode(npc);
        }

        [Fact]
        public void TestEncodeDecode_RetainsRecognizedOpcodePayloads()
        {
            var codec = new NpcTypeCodec();
            byte[] definitionBytes =
            [
                121, 1, 0, 0xFF, 2, 0xFD,
                44, 0x12, 0x34,
                45, 0xAB, 0xCD,
                170, 0, 1,
                171, 0, 2,
                172, 0, 3,
                173, 0xFF, 0xFF,
                174, 0, 5,
                175, 0, 6,
                0
            ];

            using var source = new MemoryStream(definitionBytes);
            var decoded = (NpcType)codec.Decode(1, source);
            using var encoded = codec.Encode(decoded);
            using var encodedSource = new MemoryStream(encoded.ToArray());
            var roundTripped = (NpcType)codec.Decode(1, encodedSource);

            Assert.Equal((ushort)0x1234, roundTripped.Opcode44Payload);
            Assert.Equal((ushort)0xABCD, roundTripped.Opcode45Payload);
            Assert.Equal(new ushort?[] { 1, 2, 3, ushort.MaxValue, 5, 6 }, roundTripped.Opcode170To175Payloads);
            Assert.True(roundTripped.HasAnIntArrayArray1449);
            Assert.Equal(new[] { -1, 2, -3 }, roundTripped.AnIntArrayArray1449[0]);
        }

        [Fact]
        public void TestEncodeDecode_Revision742NpcRetainsOpcode170Payload()
        {
            // Captured from index 18, archive 0, member 0 in the revision-742 cache.
            byte[] definitionBytes = Convert.FromHexString(
                "7F05925F00000C011E54616C6B2D746F0098427579206361706573002805140D1A8E192B0619151C061919A3061315940613290200FF012400FB01060248616E7300010680009D897071707C80009DB2705F708A3C0180009A977702AA002C00");
            var codec = new NpcTypeCodec();

            var decoded = (NpcType)codec.Decode(0, new MemoryStream(definitionBytes));
            using var encoded = codec.Encode(decoded);
            var roundTripped = (NpcType)codec.Decode(0, new MemoryStream(encoded.ToArray()));

            Assert.Equal((ushort)44, roundTripped.Opcode170To175Payloads![0]);
        }

        [Fact]
        public void TestEncodeDecode_Revision742NpcRetainsLatin1Name()
        {
            // Captured from NPC 11110 in the revision-742 cache.
            byte[] definitionBytes = Convert.FromHexString(
                "635D1E496E746572616374005F00007F06D98D024E61EF766520626C6F6F6472616765720001018000D8293C018000D919F90D0000028100000000000000040000000000000283000000000000000300000000000003C50000000000000006000000800000000500000080000000070000008000000009000000C000000008000000000000000E00000006000001A80000002D0000018A00000015770100");
            var codec = new NpcTypeCodec();

            var decoded = (NpcType)codec.Decode(11110, new MemoryStream(definitionBytes));
            using var encoded = codec.Encode(decoded);
            var roundTripped = (NpcType)codec.Decode(11110, new MemoryStream(encoded.ToArray()));

            Assert.Equal("Naïve bloodrager", roundTripped.Name);
        }

        [Fact]
        public void TestEncodeDecode_PreservesSpawnDirectionSeven()
        {
            var codec = new NpcTypeCodec();
            using var source = new MemoryStream([125, 7, 0]);

            var decoded = (NpcType)codec.Decode(1, source);
            using var encoded = codec.Encode(decoded);
            var roundTripped = (NpcType)codec.Decode(1, new MemoryStream(encoded.ToArray()));

            Assert.Equal((sbyte)7, roundTripped.SpawnFaceDirection);
        }

    }
}
