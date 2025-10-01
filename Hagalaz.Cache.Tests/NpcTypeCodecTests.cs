using Hagalaz.Cache.Types;
using System.Collections.Generic;
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
    }
}