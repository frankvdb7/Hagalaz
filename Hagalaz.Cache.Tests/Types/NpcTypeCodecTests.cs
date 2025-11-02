using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class NpcTypeCodecTests
    {
        [Fact]
        public void RoundTrip_WithAllProperties_ShouldSucceed()
        {
            // Arrange
            var codec = new NpcTypeCodec();
            var originalNpc = new NpcType(123)
            {
                Name = "Test NPC",
                Size = 2,
                ModelIDs = new[] { 10, 20, 30 },
                Actions = new[] { "Talk-to", "Attack", "Examine", null, null, "Examine" },
                OriginalColours = new short[] { 100, 200 },
                ModifiedColours = new short[] { 101, 201 },
                AShortArray1508 = new short[] { 300, 400 },
                AShortArray1491 = new short[] { 301, 401 },
                AByteArray1446 = new sbyte[] { 5, 6, 7 },
                InterfaceModelIDs = new[] { 40, 50 },
                VisibleOnMinimap = false,
                CombatLevel = 75,
                ScaleX = 150,
                ScaleY = 160,
                IsVisible = true,
                LightModifier = 10,
                ShadowModifier = 20,
                HeadIcon = 5,
                DegreesToTurn = 90,
                TransformToIDs = new[] { 1, 2, 3, -1 },
                VarpBitFileID = 123,
                ConfigID = 456,
                IsClickable = false,
                ABoolean1470 = false,
                ABoolean1457 = false,
                AShort1495 = 500,
                AShort1490 = 600,
                AByte1447 = 1,
                AByte1445 = 2,
                WalkingProperties = 3,
                AnIntArrayArray1449 = new[] { new[] { 1, 2, 3 }, null, new[] { 4, 5, 6 } },
                AnInt1462 = 700,
                SpawnFaceDirection = 4,
                RenderId = 800,
                Speed = 5,
                IdleAnimationId = 101,
                MoveType1AnimationId = 102,
                MoveType2AnimationId = 103,
                RunAnimationId = 104,
                AnInt1504 = 8,
                AnInt1480 = 9,
                AnInt1453 = 10,
                AnInt1510 = 11,
                AnInt1475 = 12,
                AttackCursor = 13,
                AnInt1507 = 14,
                AnInt1456 = 15,
                ABoolean1511 = true,
                MapIcon = 16,
                ABoolean1483 = true,
                ModelRedColor = 20,
                ModelGreenColor = 30,
                ModelBlueColor = 40,
                ModelAlphaColor = 50,
                AByte1487 = 1,
                QuestIDs = new[] { 1001, 1002 },
                HasDisplayName = true,
                AnInt1454 = 17,
                AnInt1502 = 257,
                AnInt1463 = 258,
                AnInt1497 = 18,
                AnInt1464 = 19,
                ExtraData = new Dictionary<int, object>
                {
                    { 1, "string value" },
                    { 2, 12345 }
                }
            };

            // Act
            var encodedStream = codec.Encode(originalNpc);
            encodedStream.Position = 0;
            var decodedNpc = (NpcType)codec.Decode(originalNpc.Id, encodedStream);

            // Assert
            Assert.Equal(originalNpc.Id, decodedNpc.Id);
            Assert.Equal(originalNpc.Name, decodedNpc.Name);
            Assert.Equal(originalNpc.Size, decodedNpc.Size);
            Assert.Equal(originalNpc.ModelIDs, decodedNpc.ModelIDs);
            Assert.Equal(originalNpc.Actions, decodedNpc.Actions);
            Assert.Equal(originalNpc.OriginalColours, decodedNpc.OriginalColours);
            Assert.Equal(originalNpc.ModifiedColours, decodedNpc.ModifiedColours);
            Assert.Equal(originalNpc.AShortArray1508, decodedNpc.AShortArray1508);
            Assert.Equal(originalNpc.AShortArray1491, decodedNpc.AShortArray1491);
            Assert.Equal(originalNpc.AByteArray1446, decodedNpc.AByteArray1446);
            Assert.Equal(originalNpc.InterfaceModelIDs, decodedNpc.InterfaceModelIDs);
            Assert.Equal(originalNpc.VisibleOnMinimap, decodedNpc.VisibleOnMinimap);
            Assert.Equal(originalNpc.CombatLevel, decodedNpc.CombatLevel);
            Assert.Equal(originalNpc.ScaleX, decodedNpc.ScaleX);
            Assert.Equal(originalNpc.ScaleY, decodedNpc.ScaleY);
            Assert.Equal(originalNpc.IsVisible, decodedNpc.IsVisible);
            Assert.Equal(originalNpc.LightModifier, decodedNpc.LightModifier);
            Assert.Equal(originalNpc.ShadowModifier, decodedNpc.ShadowModifier);
            Assert.Equal(originalNpc.HeadIcon, decodedNpc.HeadIcon);
            Assert.Equal(originalNpc.DegreesToTurn, decodedNpc.DegreesToTurn);
            Assert.Equal(originalNpc.TransformToIDs, decodedNpc.TransformToIDs);
            Assert.Equal(originalNpc.VarpBitFileID, decodedNpc.VarpBitFileID);
            Assert.Equal(originalNpc.ConfigID, decodedNpc.ConfigID);
            Assert.Equal(originalNpc.IsClickable, decodedNpc.IsClickable);
            Assert.Equal(originalNpc.ABoolean1470, decodedNpc.ABoolean1470);
            Assert.Equal(originalNpc.ABoolean1457, decodedNpc.ABoolean1457);
            Assert.Equal(originalNpc.AShort1495, decodedNpc.AShort1495);
            Assert.Equal(originalNpc.AShort1490, decodedNpc.AShort1490);
            Assert.Equal(originalNpc.AByte1447, decodedNpc.AByte1447);
            Assert.Equal(originalNpc.AByte1445, decodedNpc.AByte1445);
            Assert.Equal(originalNpc.WalkingProperties, decodedNpc.WalkingProperties);
            Assert.Equal(originalNpc.AnIntArrayArray1449, decodedNpc.AnIntArrayArray1449);
            Assert.Equal(originalNpc.AnInt1462, decodedNpc.AnInt1462);
            Assert.Equal(originalNpc.SpawnFaceDirection, decodedNpc.SpawnFaceDirection);
            Assert.Equal(originalNpc.RenderId, decodedNpc.RenderId);
            Assert.Equal(originalNpc.Speed, decodedNpc.Speed);
            Assert.Equal(originalNpc.IdleAnimationId, decodedNpc.IdleAnimationId);
            Assert.Equal(originalNpc.MoveType1AnimationId, decodedNpc.MoveType1AnimationId);
            Assert.Equal(originalNpc.MoveType2AnimationId, decodedNpc.MoveType2AnimationId);
            Assert.Equal(originalNpc.RunAnimationId, decodedNpc.RunAnimationId);
            Assert.Equal(originalNpc.AnInt1504, decodedNpc.AnInt1504);
            Assert.Equal(originalNpc.AnInt1480, decodedNpc.AnInt1480);
            Assert.Equal(originalNpc.AnInt1453, decodedNpc.AnInt1453);
            Assert.Equal(originalNpc.AnInt1510, decodedNpc.AnInt1510);
            Assert.Equal(originalNpc.AnInt1475, decodedNpc.AnInt1475);
            Assert.Equal(originalNpc.AttackCursor, decodedNpc.AttackCursor);
            Assert.Equal(originalNpc.AnInt1507, decodedNpc.AnInt1507);
            Assert.Equal(originalNpc.AnInt1456, decodedNpc.AnInt1456);
            Assert.Equal(originalNpc.ABoolean1511, decodedNpc.ABoolean1511);
            Assert.Equal(originalNpc.MapIcon, decodedNpc.MapIcon);
            Assert.Equal(originalNpc.ABoolean1483, decodedNpc.ABoolean1483);
            Assert.Equal(originalNpc.ModelRedColor, decodedNpc.ModelRedColor);
            Assert.Equal(originalNpc.ModelGreenColor, decodedNpc.ModelGreenColor);
            Assert.Equal(originalNpc.ModelBlueColor, decodedNpc.ModelBlueColor);
            Assert.Equal(originalNpc.ModelAlphaColor, decodedNpc.ModelAlphaColor);
            Assert.Equal(originalNpc.AByte1487, decodedNpc.AByte1487);
            Assert.Equal(originalNpc.QuestIDs, decodedNpc.QuestIDs);
            Assert.Equal(originalNpc.HasDisplayName, decodedNpc.HasDisplayName);
            Assert.Equal(originalNpc.AnInt1454, decodedNpc.AnInt1454);
            Assert.Equal(originalNpc.AnInt1502, decodedNpc.AnInt1502);
            Assert.Equal(originalNpc.AnInt1463, decodedNpc.AnInt1463);
            Assert.Equal(originalNpc.AnInt1497, decodedNpc.AnInt1497);
            Assert.Equal(originalNpc.AnInt1464, decodedNpc.AnInt1464);
            Assert.Equal(originalNpc.ExtraData, decodedNpc.ExtraData);
        }
    }
}