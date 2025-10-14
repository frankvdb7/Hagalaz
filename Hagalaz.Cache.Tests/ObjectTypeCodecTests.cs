using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ObjectTypeCodecTests
    {
        [Fact]
        public void Encode_Decode_RoundTrip_ShouldRestoreSameData()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var originalObject = new ObjectType(123)
            {
                Name = "Test Object",
                SizeX = 2,
                SizeY = 3,
                Solid = false,
                Interactable = 1,
                Actions = new[] { "Action1", "Action2", null, null, null, "Examine" },
                AnimationIDs = new[] { 1, 2, 3 },
                AnIntArray784 = new[] { 10, 20, 30 },
                ExtraData = new Dictionary<int, object>
                {
                    { 1, "string value" },
                    { 2, 12345 }
                }
            };

            // Act
            var encodedStream = codec.Encode(originalObject);
            encodedStream.Position = 0;
            var decodedObject = (ObjectType)codec.Decode(originalObject.Id, encodedStream);

            // Assert
            Assert.Equal(originalObject.Id, decodedObject.Id);
            Assert.Equal(originalObject.Name, decodedObject.Name);
            Assert.Equal(originalObject.SizeX, decodedObject.SizeX);
            Assert.Equal(originalObject.SizeY, decodedObject.SizeY);
            Assert.Equal(originalObject.Solid, decodedObject.Solid);
            Assert.Equal(originalObject.Interactable, decodedObject.Interactable);
            Assert.Equal(originalObject.Actions, decodedObject.Actions);
            Assert.Equal(originalObject.AnimationIDs, decodedObject.AnimationIDs);
            Assert.NotNull(decodedObject.AnIntArray784);
            Assert.Equal(originalObject.AnIntArray784.Length, decodedObject.AnIntArray784.Length);
            Assert.Equal(originalObject.ExtraData, decodedObject.ExtraData);
        }

        [Fact]
        public void Decode_WithEmptyStream_ShouldReturnDefaultObject()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(0);
            stream.Position = 0;
            var defaultObject = new ObjectType(456);

            // Act
            var decodedObject = (ObjectType)codec.Decode(456, stream);

            // Assert
            Assert.Equal(defaultObject.Id, decodedObject.Id);
            Assert.Equal(defaultObject.Name, decodedObject.Name);
            Assert.Equal(defaultObject.SizeX, decodedObject.SizeX);
        }

        [Fact]
        public void Encode_Decode_ClipType1_ShouldRestoreSameData()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var originalObject = new ObjectType(1) { ClipType = 1 };

            // Act
            var encodedStream = codec.Encode(originalObject);
            encodedStream.Position = 0;
            var decodedObject = (ObjectType)codec.Decode(originalObject.Id, encodedStream);

            // Assert
            Assert.Equal(originalObject.ClipType, decodedObject.ClipType);
        }

        [Fact]
        public void Encode_Decode_GroundContoured_ShouldRestoreSameData()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var originalObject = new ObjectType(1) { GroundContoured = 1 };

            // Act
            var encodedStream = codec.Encode(originalObject);
            encodedStream.Position = 0;
            var decodedObject = (ObjectType)codec.Decode(originalObject.Id, encodedStream);

            // Assert
            Assert.Equal(originalObject.GroundContoured, decodedObject.GroundContoured);
        }

        [Fact]
        public void Encode_Decode_ComplexObject_ShouldRestoreSameData()
        {
            // Arrange
            var codec = new ObjectTypeCodec();
            var originalObject = new ObjectType(1)
            {
                ModelIDs = new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 } },
                Shapes = new sbyte[] { 1, 2 },
                Name = "Complex Object",
                SizeX = 5,
                SizeY = 10,
                ClipType = 0,
                Solid = false,
                Interactable = 0,
                GroundContoured = 1,
                DelayShading = true,
                AnimationIDs = new int[] { 123, 456, 789 },
                AnIntArray784 = new int[] { 1, 2, 3 },
                DecorDisplacement = 128,
                Ambient = 10,
                Contrast = 20,
                Actions = new[] { "Examine", null, "Use", null, null, "Examine" },
                OriginalColors = new short[] { 100, 200 },
                ModifiedColors = new short[] { 300, 400 },
                AShortArray831 = new short[] { 1, 2 },
                AShortArray762 = new short[] { 3, 4 },
                AByteArray816 = new sbyte[] { -1, -2 },
                Inverted = true,
                CastsShadow = false,
                ScaleX = 256,
                ScaleY = 256,
                ScaleZ = 256,
                Surroundings = 1,
                OffsetX = 4,
                OffsetY = 8,
                OffsetZ = 12,
                ObstructsGround = true,
                Gateway = true,
                SupportItemsFlag = 1,
                AmbientSoundID = 456,
                AmbientSoundHearDistance = 5,
                AudioTracks = new int[] { 7, 8, 9 },
                AnInt833 = 10,
                AnInt768 = 20,
                Hidden = true,
                ABoolean779 = false,
                ABoolean838 = false,
                MembersOnly = true,
                VarpBitFileId = 12,
                VarpFileId = 34,
                TransformToIDs = new int[] { 101, 102, 103, -1 },
                AdjustMapSceneRotation = true,
                HasAnimation = true,
                AnInt788 = 5,
                AnInt827 = 6,
                AnInt764 = 7,
                AnInt828 = 8,
                MapSpriteRotation = 90,
                MapSpriteType = 11,
                Occludes = 0,
                AmbientSoundVolume = 200,
                FlipMapSprite = true,
                MapIcon = 14,
                QuestIDs = new int[] { 1, 2, 3 },
                AByte826 = -5,
                AByte790 = -6,
                AByte821 = -7,
                AByte787 = -8,
                AnInt782 = 32767,
                AnInt830 = -32768,
                AnInt778 = 12345,
                AnInt776 = 54321,
                ABoolean810 = true,
                ABoolean781 = true,
                AnInt823 = 1000,
                AnInt773 = 2000,
                AnInt825 = 512,
                AnInt808 = 512,
                ABoolean835 = true,
                AnInt813 = 1,
                ExtraData = new Dictionary<int, object> { { 1, "test" }, { 2, 123 } }
            };

            // Act
            var encodedStream = codec.Encode(originalObject);
            encodedStream.Position = 0;
            var decodedObject = (ObjectType)codec.Decode(originalObject.Id, encodedStream);

            // Assert
            Assert.Equal(originalObject.Id, decodedObject.Id);
            Assert.Equal(originalObject.Name, decodedObject.Name);
            Assert.Equal(originalObject.SizeX, decodedObject.SizeX);
            Assert.Equal(originalObject.SizeY, decodedObject.SizeY);
            Assert.Equal(originalObject.Solid, decodedObject.Solid);
            Assert.Equal(originalObject.Interactable, decodedObject.Interactable);
            Assert.Equal(originalObject.GroundContoured, decodedObject.GroundContoured);
            Assert.Equal(originalObject.DelayShading, decodedObject.DelayShading);
            Assert.Equal(originalObject.Occludes, decodedObject.Occludes);
            Assert.Equal(originalObject.AnimationIDs, decodedObject.AnimationIDs);
            Assert.Equal(originalObject.DecorDisplacement, decodedObject.DecorDisplacement);
            Assert.Equal(originalObject.Ambient, decodedObject.Ambient);
            Assert.Equal(originalObject.Contrast, decodedObject.Contrast);
            Assert.Equal(originalObject.Actions, decodedObject.Actions);
            Assert.Equal(originalObject.OriginalColors, decodedObject.OriginalColors);
            Assert.Equal(originalObject.ModifiedColors, decodedObject.ModifiedColors);
            Assert.Equal(originalObject.AShortArray831, decodedObject.AShortArray831);
            Assert.Equal(originalObject.AShortArray762, decodedObject.AShortArray762);
            Assert.Equal(originalObject.AByteArray816, decodedObject.AByteArray816);
            Assert.Equal(originalObject.Inverted, decodedObject.Inverted);
            Assert.Equal(originalObject.CastsShadow, decodedObject.CastsShadow);
            Assert.Equal(originalObject.ScaleX, decodedObject.ScaleX);
            Assert.Equal(originalObject.ScaleY, decodedObject.ScaleY);
            Assert.Equal(originalObject.ScaleZ, decodedObject.ScaleZ);
            Assert.Equal(originalObject.Surroundings, decodedObject.Surroundings);
            Assert.Equal(originalObject.OffsetX, decodedObject.OffsetX);
            Assert.Equal(originalObject.OffsetY, decodedObject.OffsetY);
            Assert.Equal(originalObject.OffsetZ, decodedObject.OffsetZ);
            Assert.Equal(originalObject.ObstructsGround, decodedObject.ObstructsGround);
            Assert.Equal(originalObject.Gateway, decodedObject.Gateway);
            Assert.Equal(originalObject.SupportItemsFlag, decodedObject.SupportItemsFlag);
            Assert.Equal(originalObject.AmbientSoundID, decodedObject.AmbientSoundID);
            Assert.Equal(originalObject.AmbientSoundHearDistance, decodedObject.AmbientSoundHearDistance);
            Assert.Equal(originalObject.AudioTracks, decodedObject.AudioTracks);
            Assert.Equal(originalObject.AnInt833, decodedObject.AnInt833);
            Assert.Equal(originalObject.AnInt768, decodedObject.AnInt768);
            Assert.Equal(originalObject.Hidden, decodedObject.Hidden);
            Assert.Equal(originalObject.ABoolean779, decodedObject.ABoolean779);
            Assert.Equal(originalObject.ABoolean838, decodedObject.ABoolean838);
            Assert.Equal(originalObject.MembersOnly, decodedObject.MembersOnly);
            Assert.Equal(originalObject.VarpBitFileId, decodedObject.VarpBitFileId);
            Assert.Equal(originalObject.VarpFileId, decodedObject.VarpFileId);
            Assert.Equal(originalObject.TransformToIDs, decodedObject.TransformToIDs);
            Assert.Equal(originalObject.AdjustMapSceneRotation, decodedObject.AdjustMapSceneRotation);
            Assert.Equal(originalObject.HasAnimation, decodedObject.HasAnimation);
            Assert.Equal(originalObject.AnInt788, decodedObject.AnInt788);
            Assert.Equal(originalObject.AnInt827, decodedObject.AnInt827);
            Assert.Equal(originalObject.AnInt764, decodedObject.AnInt764);
            Assert.Equal(originalObject.AnInt828, decodedObject.AnInt828);
            Assert.Equal(originalObject.MapSpriteRotation, decodedObject.MapSpriteRotation);
            Assert.Equal(originalObject.MapSpriteType, decodedObject.MapSpriteType);
            Assert.Equal(originalObject.AmbientSoundVolume, decodedObject.AmbientSoundVolume);
            Assert.Equal(originalObject.FlipMapSprite, decodedObject.FlipMapSprite);
            Assert.NotNull(decodedObject.AnIntArray784);
            Assert.Equal(originalObject.AnIntArray784.Length, decodedObject.AnIntArray784.Length);
            Assert.Equal(originalObject.MapIcon, decodedObject.MapIcon);
            Assert.Equal(originalObject.QuestIDs, decodedObject.QuestIDs);
            Assert.Equal(originalObject.AByte826, decodedObject.AByte826);
            Assert.Equal(originalObject.AByte790, decodedObject.AByte790);
            Assert.Equal(originalObject.AByte821, decodedObject.AByte821);
            Assert.Equal(originalObject.AByte787, decodedObject.AByte787);
            Assert.Equal(originalObject.AnInt782, decodedObject.AnInt782);
            Assert.Equal(originalObject.AnInt830, decodedObject.AnInt830);
            Assert.Equal(originalObject.AnInt778, decodedObject.AnInt778);
            Assert.Equal(originalObject.AnInt776, decodedObject.AnInt776);
            Assert.Equal(originalObject.ABoolean810, decodedObject.ABoolean810);
            Assert.Equal(originalObject.ABoolean781, decodedObject.ABoolean781);
            Assert.Equal(originalObject.AnInt823, decodedObject.AnInt823);
            Assert.Equal(originalObject.AnInt773, decodedObject.AnInt773);
            Assert.Equal(originalObject.AnInt825, decodedObject.AnInt825);
            Assert.Equal(originalObject.AnInt808, decodedObject.AnInt808);
            Assert.Equal(originalObject.ABoolean835, decodedObject.ABoolean835);
            Assert.Equal(originalObject.AnInt813, decodedObject.AnInt813);
            Assert.Equal(originalObject.ExtraData, decodedObject.ExtraData);
        }
    }
}