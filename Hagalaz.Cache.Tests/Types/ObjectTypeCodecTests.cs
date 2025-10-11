using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Moq;
using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class ObjectTypeCodecTests
    {
        [Fact]
        public void DecodeAndEncode_Symmetric()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var objectType = new ObjectType(1)
            {
                Name = "Test Object",
                SizeX = 2,
                SizeY = 3,
                Solid = true,
                Interactable = 1,
                ClipType = 1,
                Actions = new[] { "Examine", null, null, null, null }
            };

            // Act
            var encodedStream = codec.Encode(objectType);
            encodedStream.Position = 0;
            var decodedObjectType = codec.Decode(1, encodedStream);

            // Assert
            Assert.Equal(objectType.Name, decodedObjectType.Name);
            Assert.Equal(objectType.SizeX, decodedObjectType.SizeX);
            Assert.Equal(objectType.SizeY, decodedObjectType.SizeY);
            Assert.Equal(objectType.Solid, decodedObjectType.Solid);
            Assert.Equal(objectType.Interactable, ((ObjectType)decodedObjectType).Interactable);
            Assert.Equal(objectType.ClipType, decodedObjectType.ClipType);
            Assert.Equal(objectType.Actions[0], decodedObjectType.Actions[0]);
        }

        [Fact]
        public void DecodeAndEncode_AllFields()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var objectType = new ObjectType(1)
            {
                ModelIDs = new int[][] { new int[] { 10, 20 }, new int[] { 30 } },
                Shapes = new sbyte[] { 1, 2 },
                Name = "Complex Object",
                SizeX = 2,
                SizeY = 3,
                ClipType = 1,
                Solid = true,
                Interactable = 1,
                DelayShading = true,
                AnimationIDs = new int[] { 123 },
                DecorDisplacement = 128,
                Ambient = 10,
                Contrast = 20,
                Actions = new[] { "Action1", "Action2", "Action3", "Action4", "Action5" },
                OriginalColors = new short[] { 100, 200 },
                ModifiedColors = new short[] { 101, 201 },
                AShortArray831 = new short[] { 300, 400 },
                AShortArray762 = new short[] { 301, 401 },
                AByteArray816 = new sbyte[] { 5, 6 },
                Inverted = true,
                CastsShadow = false,
                ScaleX = 256,
                ScaleY = 256,
                ScaleZ = 256,
                Surroundings = 5,
                OffsetX = 4,
                OffsetY = 8,
                OffsetZ = 12,
                ObstructsGround = true,
                Gateway = true,
                SupportItemsFlag = 2,
                AmbientSoundID = 456,
                AmbientSoundHearDistance = 10,
                AudioTracks = new int[] { 789, 101, 112 },
                AnInt833 = 1,
                AnInt768 = 2,
                Hidden = true,
                ABoolean779 = false,
                ABoolean838 = false,
                MembersOnly = true,
                TransformToIDs = new int[] { 1, 2, 3 },
                VarpBitFileId = 4,
                VarpFileId = 5,
                GroundContoured = 3,
                AnInt780 = 500,
                AdjustMapSceneRotation = true,
                HasAnimation = true,
                AnInt788 = 6,
                AnInt827 = 7,
                AnInt764 = 8,
                AnInt828 = 9,
                MapSpriteRotation = 90,
                MapSpriteType = 11,
                Occludes = 0,
                AmbientSoundVolume = 200,
                FlipMapSprite = true,
                MapIcon = 12,
                QuestIDs = new int[] { 13, 14 },
                AByte826 = 15,
                AByte790 = 16,
                AByte821 = 17,
                AByte787 = 18,
                AnInt782 = 19,
                AnInt830 = 20,
                AnInt778 = 21,
                AnInt776 = 22,
                ABoolean810 = true,
                ABoolean781 = true,
                AnInt823 = 961,
                AnInt773 = 23,
                AnInt825 = 257,
                AnInt808 = 258,
                ABoolean835 = true,
                AnInt813 = 24,
                ExtraData = new System.Collections.Generic.Dictionary<int, object>
                {
                    { 1, 100 },
                    { 2, "test" }
                }
            };

            // Act
            var encodedStream = codec.Encode(objectType);
            encodedStream.Position = 0;
            var decodedObjectType = codec.Decode(1, encodedStream) as ObjectType;

            // Assert
            Assert.NotNull(decodedObjectType);
            Assert.Equal(objectType.Name, decodedObjectType.Name);
            Assert.Equal(objectType.SizeX, decodedObjectType.SizeX);
            Assert.Equal(objectType.SizeY, decodedObjectType.SizeY);
            Assert.Equal(objectType.Solid, decodedObjectType.Solid);
            Assert.Equal(objectType.Interactable, decodedObjectType.Interactable);
            Assert.Equal(objectType.ClipType, decodedObjectType.ClipType);
            Assert.Equal(objectType.Actions[0], decodedObjectType.Actions[0]);
            Assert.Equal(objectType.ModelIDs.Length, decodedObjectType.ModelIDs.Length);
            Assert.Equal(objectType.ModelIDs[0][0], decodedObjectType.ModelIDs[0][0]);
            Assert.Equal(objectType.Shapes.Length, decodedObjectType.Shapes.Length);
            Assert.Equal(objectType.Shapes[0], decodedObjectType.Shapes[0]);
            Assert.Equal(objectType.GroundContoured, decodedObjectType.GroundContoured);
            Assert.Equal(objectType.DelayShading, decodedObjectType.DelayShading);
            Assert.Equal(objectType.Occludes, decodedObjectType.Occludes);
            Assert.Equal(objectType.AnimationIDs[0], decodedObjectType.AnimationIDs[0]);
            Assert.Equal(objectType.DecorDisplacement, decodedObjectType.DecorDisplacement);
            Assert.Equal(objectType.Ambient, decodedObjectType.Ambient);
            Assert.Equal(objectType.Contrast, decodedObjectType.Contrast);
            Assert.Equal(objectType.OriginalColors.Length, decodedObjectType.OriginalColors.Length);
            Assert.Equal(objectType.OriginalColors[0], decodedObjectType.OriginalColors[0]);
            Assert.Equal(objectType.ModifiedColors.Length, decodedObjectType.ModifiedColors.Length);
            Assert.Equal(objectType.ModifiedColors[0], decodedObjectType.ModifiedColors[0]);
            Assert.Equal(objectType.AShortArray831.Length, decodedObjectType.AShortArray831.Length);
            Assert.Equal(objectType.AShortArray831[0], decodedObjectType.AShortArray831[0]);
            Assert.Equal(objectType.AShortArray762.Length, decodedObjectType.AShortArray762.Length);
            Assert.Equal(objectType.AShortArray762[0], decodedObjectType.AShortArray762[0]);
            Assert.Equal(objectType.AByteArray816.Length, decodedObjectType.AByteArray816.Length);
            Assert.Equal(objectType.AByteArray816[0], decodedObjectType.AByteArray816[0]);
            Assert.Equal(objectType.Inverted, decodedObjectType.Inverted);
            Assert.Equal(objectType.CastsShadow, decodedObjectType.CastsShadow);
            Assert.Equal(objectType.ScaleX, decodedObjectType.ScaleX);
            Assert.Equal(objectType.ScaleY, decodedObjectType.ScaleY);
            Assert.Equal(objectType.ScaleZ, decodedObjectType.ScaleZ);
            Assert.Equal(objectType.Surroundings, decodedObjectType.Surroundings);
            Assert.Equal(objectType.OffsetX, decodedObjectType.OffsetX);
            Assert.Equal(objectType.OffsetY, decodedObjectType.OffsetY);
            Assert.Equal(objectType.OffsetZ, decodedObjectType.OffsetZ);
            Assert.Equal(objectType.ObstructsGround, decodedObjectType.ObstructsGround);
            Assert.Equal(objectType.Gateway, decodedObjectType.Gateway);
            Assert.Equal(objectType.SupportItemsFlag, decodedObjectType.SupportItemsFlag);
            Assert.Equal(objectType.AmbientSoundID, decodedObjectType.AmbientSoundID);
            Assert.Equal(objectType.AmbientSoundHearDistance, decodedObjectType.AmbientSoundHearDistance);
            Assert.Equal(objectType.AudioTracks.Length, decodedObjectType.AudioTracks.Length);
            Assert.Equal(objectType.AudioTracks[0], decodedObjectType.AudioTracks[0]);
            Assert.Equal(objectType.AnInt833, decodedObjectType.AnInt833);
            Assert.Equal(objectType.AnInt768, decodedObjectType.AnInt768);
            Assert.Equal(objectType.Hidden, decodedObjectType.Hidden);
            Assert.Equal(objectType.ABoolean779, decodedObjectType.ABoolean779);
            Assert.Equal(objectType.ABoolean838, decodedObjectType.ABoolean838);
            Assert.Equal(objectType.MembersOnly, decodedObjectType.MembersOnly);
            Assert.Equal(objectType.TransformToIDs.Length, decodedObjectType.TransformToIDs.Length);
            Assert.Equal(objectType.TransformToIDs[0], decodedObjectType.TransformToIDs[0]);
            Assert.Equal(objectType.VarpBitFileId, decodedObjectType.VarpBitFileId);
            Assert.Equal(objectType.VarpFileId, decodedObjectType.VarpFileId);
            Assert.Equal(objectType.AnInt780, decodedObjectType.AnInt780);
            Assert.Equal(objectType.AdjustMapSceneRotation, decodedObjectType.AdjustMapSceneRotation);
            Assert.Equal(objectType.HasAnimation, decodedObjectType.HasAnimation);
            Assert.Equal(objectType.AnInt788, decodedObjectType.AnInt788);
            Assert.Equal(objectType.AnInt827, decodedObjectType.AnInt827);
            Assert.Equal(objectType.AnInt764, decodedObjectType.AnInt764);
            Assert.Equal(objectType.AnInt828, decodedObjectType.AnInt828);
            Assert.Equal(objectType.MapSpriteRotation, decodedObjectType.MapSpriteRotation);
            Assert.Equal(objectType.MapSpriteType, decodedObjectType.MapSpriteType);
            Assert.Equal(objectType.AmbientSoundVolume, decodedObjectType.AmbientSoundVolume);
            Assert.Equal(objectType.FlipMapSprite, decodedObjectType.FlipMapSprite);
            Assert.Equal(objectType.MapIcon, decodedObjectType.MapIcon);
            Assert.Equal(objectType.QuestIDs.Length, decodedObjectType.QuestIDs.Length);
            Assert.Equal(objectType.QuestIDs[0], decodedObjectType.QuestIDs[0]);
            Assert.Equal(objectType.AByte826, decodedObjectType.AByte826);
            Assert.Equal(objectType.AByte790, decodedObjectType.AByte790);
            Assert.Equal(objectType.AByte821, decodedObjectType.AByte821);
            Assert.Equal(objectType.AByte787, decodedObjectType.AByte787);
            Assert.Equal(objectType.AnInt782, decodedObjectType.AnInt782);
            Assert.Equal(objectType.AnInt830, decodedObjectType.AnInt830);
            Assert.Equal(objectType.AnInt778, decodedObjectType.AnInt778);
            Assert.Equal(objectType.AnInt776, decodedObjectType.AnInt776);
            Assert.Equal(objectType.ABoolean810, decodedObjectType.ABoolean810);
            Assert.Equal(objectType.ABoolean781, decodedObjectType.ABoolean781);
            Assert.Equal(objectType.AnInt823, decodedObjectType.AnInt823);
            Assert.Equal(objectType.AnInt773, decodedObjectType.AnInt773);
            Assert.Equal(objectType.AnInt825, decodedObjectType.AnInt825);
            Assert.Equal(objectType.AnInt808, decodedObjectType.AnInt808);
            Assert.Equal(objectType.ABoolean835, decodedObjectType.ABoolean835);
            Assert.Equal(objectType.AnInt813, decodedObjectType.AnInt813);
            Assert.Equal(objectType.ExtraData.Count, decodedObjectType.ExtraData.Count);
            Assert.Equal(objectType.ExtraData[1], decodedObjectType.ExtraData[1]);
            Assert.Equal(objectType.ExtraData[2], decodedObjectType.ExtraData[2]);
        }
    }
}