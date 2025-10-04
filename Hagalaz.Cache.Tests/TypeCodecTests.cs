// Copyright (c) Geta IM. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class TypeCodecTests
{
    [Fact]
    public void ObjectTypeCodec_CanDecodeAudioTracks()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(79); // Opcode for AudioTracks
        stream.WriteShort(123); // AnInt833
        stream.WriteShort(456); // AnInt768
        stream.WriteByte(50); // AmbientSoundHearDistance
        stream.WriteByte(2); // AudioTracks length
        stream.WriteShort(10); // track 1
        stream.WriteShort(20); // track 2
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(123, decodedObject.AnInt833);
        Assert.Equal(456, decodedObject.AnInt768);
        Assert.Equal(50, decodedObject.AmbientSoundHearDistance);
        Assert.NotNull(decodedObject.AudioTracks);
        Assert.Equal(2, decodedObject.AudioTracks.Length);
        Assert.Equal(10, decodedObject.AudioTracks[0]);
        Assert.Equal(20, decodedObject.AudioTracks[1]);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeObjectWithNullActions()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        var originalObject = new ObjectType(1)
        {
            Actions = new string?[] { null, null, null, null, null, null }
        };

        // Act
        var encodedStream = codec.Encode(originalObject);
        encodedStream.Position = 0;
        var decodedObject = (ObjectType)codec.Decode(1, encodedStream);

        // Assert
        Assert.Equal(new string?[] { null, null, null, null, null, "Examine" }, decodedObject.Actions);
    }

    [Fact]
    public void ItemTypeCodec_ShouldDecodeAndEncodeComplexItem()
    {
        // Arrange
        var codec = new ItemTypeCodec();
        var originalItem = new ItemType(1)
        {
            ModelZoom = 1234,
            ModelRotation1 = 100,
            ModelRotation2 = 200,
            ModelOffset1 = 5,
            ModelOffset2 = -5,
            StackableType = 1,
            Value = 5000,
            MembersOnly = true,
            MaleWornModelId1 = 10,
            FemaleWornModelId1 = 11
        };

        // Act
        var encodedStream = codec.Encode(originalItem);
        encodedStream.Position = 0;
        var decodedItem = (ItemType)codec.Decode(1, encodedStream);

        // Assert
        Assert.Equal(originalItem.ModelZoom, decodedItem.ModelZoom);
        Assert.Equal(originalItem.ModelRotation1, decodedItem.ModelRotation1);
        Assert.Equal(originalItem.ModelRotation2, decodedItem.ModelRotation2);
        Assert.Equal(originalItem.ModelOffset1, decodedItem.ModelOffset1);
        Assert.Equal(originalItem.ModelOffset2, decodedItem.ModelOffset2);
        Assert.Equal(originalItem.StackableType, decodedItem.StackableType);
        Assert.Equal(originalItem.Value, decodedItem.Value);
        Assert.Equal(originalItem.MembersOnly, decodedItem.MembersOnly);
        Assert.Equal(originalItem.MaleWornModelId1, decodedItem.MaleWornModelId1);
        Assert.Equal(originalItem.FemaleWornModelId1, decodedItem.FemaleWornModelId1);
    }

    [Theory]
    [InlineData(15, 5)]
    public void ObjectTypeCodec_CanDecodeSizeY(byte opcode, int expectedSize)
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(opcode);
        stream.WriteByte((byte)expectedSize);
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(expectedSize, decodedObject.SizeY);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeClipTypeSolidFalse()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(17); // Opcode for ClipTypeSolidFalse
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(0, decodedObject.ClipType);
        Assert.False(decodedObject.Solid);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeSolidFalse()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(18); // Opcode for SolidFalse
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.False(decodedObject.Solid);
    }

    [Theory]
    [InlineData(19, 123)]
    public void ObjectTypeCodec_CanDecodeInteractable(byte opcode, int expectedValue)
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(opcode);
        stream.WriteByte((byte)expectedValue);
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(expectedValue, decodedObject.Interactable);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeGroundContoured1()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(21); // Opcode for GroundContoured1
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(1, decodedObject.GroundContoured);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeDelayShading()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(22); // Opcode for DelayShading
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.True(decodedObject.DelayShading);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeOccludes1()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(23); // Opcode for Occludes1
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(1, decodedObject.Occludes);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeClipType1()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(24); // Opcode for ClipType1
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(1, decodedObject.ClipType);
    }

    [Theory]
    [InlineData(25, 10)]
    public void ObjectTypeCodec_CanDecodeDecorDisplacement(byte opcode, int displacement)
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(opcode);
        stream.WriteByte((byte)displacement);
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(displacement << 2, decodedObject.DecorDisplacement);
    }

    [Theory]
    [InlineData(26, 10)]
    public void ObjectTypeCodec_CanDecodeAmbient(byte opcode, sbyte ambient)
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(opcode);
        stream.WriteSignedByte(ambient);
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(ambient, decodedObject.Ambient);
    }

    [Theory]
    [InlineData(27, 10)]
    public void ObjectTypeCodec_CanDecodeContrast(byte opcode, sbyte contrast)
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(opcode);
        stream.WriteSignedByte(contrast);
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.Equal(contrast * 5, decodedObject.Contrast);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeInverted()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(65); // Opcode for Inverted
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.True(decodedObject.Inverted);
    }

    [Fact]
    public void ObjectTypeCodec_CanDecodeCastsShadowFalse()
    {
        // Arrange
        var codec = new ObjectTypeCodec();
        using var stream = new MemoryStream();
        stream.WriteByte(66); // Opcode for CastsShadowFalse
        stream.WriteByte(0); // End of data
        stream.Position = 0;

        // Act
        var decodedObject = (ObjectType)codec.Decode(1, stream);

        // Assert
        Assert.False(decodedObject.CastsShadow);
    }
}