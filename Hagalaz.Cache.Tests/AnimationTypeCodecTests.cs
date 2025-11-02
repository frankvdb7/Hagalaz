using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class AnimationTypeCodecTests
    {
        [Fact]
        public void Decode_WithEmptyStream_ShouldReturnDefaultAnimation()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(0); // End of data opcode
            stream.Position = 0;
            var defaultAnimation = new AnimationType(1);

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.Equal(defaultAnimation.Id, decodedAnimation.Id);
            Assert.Equal(defaultAnimation.Loop, decodedAnimation.Loop);
            Assert.Equal(defaultAnimation.Priority, decodedAnimation.Priority);
            Assert.Equal(defaultAnimation.ReplayMode, decodedAnimation.ReplayMode);
        }

        [Fact]
        public void Decode_WithOpcode1_ShouldDecodeFramesAndDelays()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(1); // Opcode 1
            stream.WriteShort(2); // Count
            // Delays
            stream.WriteShort(10);
            stream.WriteShort(20);
            // Frames (low)
            stream.WriteShort(1);
            stream.WriteShort(2);
            // Frames (high)
            stream.WriteShort(3);
            stream.WriteShort(4);
            stream.WriteByte(0); // End of data
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.NotNull(decodedAnimation.Delays);
            Assert.Equal(2, decodedAnimation.Delays.Length);
            Assert.Equal(10, decodedAnimation.Delays[0]);
            Assert.Equal(20, decodedAnimation.Delays[1]);

            Assert.NotNull(decodedAnimation.Frames);
            Assert.Equal(2, decodedAnimation.Frames.Length);
            Assert.Equal((3 << 16) | 1, decodedAnimation.Frames[0]);
            Assert.Equal((4 << 16) | 2, decodedAnimation.Frames[1]);
        }

        [Theory]
        [InlineData(2, 999, "Loop")]
        [InlineData(5, 10, "Priority")]
        [InlineData(6, 123, "ShieldDisplayedItemId")]
        [InlineData(7, 456, "WeaponDisplayedItemId")]
        [InlineData(8, 50, "Cycles")]
        [InlineData(9, 3, "AnimationMode")]
        [InlineData(10, 4, "WalkingProperties")]
        [InlineData(11, 1, "ReplayMode")]
        public void Decode_SimpleOpcodes_ShouldSetCorrectProperties(int opcode, int value, string propertyName)
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte((byte)opcode);
            if (opcode == 2 || opcode == 6 || opcode == 7)
                stream.WriteShort(value);
            else
                stream.WriteByte((byte)value);
            stream.WriteByte(0);
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);
            var property = typeof(AnimationType).GetProperty(propertyName);

            // Assert
            Assert.NotNull(property);
            Assert.Equal(value, property.GetValue(decodedAnimation));
        }

        [Fact]
        public void Decode_WithOpcode13_ShouldDecodeSoundSettings()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(13); // Opcode
            stream.WriteShort(2); // Sound settings count
            // Setting 1
            stream.WriteByte(2); // Inner array size
            stream.WriteMedInt(12345); // Value 1
            stream.WriteShort(100);   // Value 2
            // Setting 2
            stream.WriteByte(1); // Inner array size
            stream.WriteMedInt(54321); // Value 1
            stream.WriteByte(0); // End of data
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.NotNull(decodedAnimation.SoundSettings);
            Assert.Equal(2, decodedAnimation.SoundSettings.Length);
            Assert.NotNull(decodedAnimation.SoundSettings[0]);
            Assert.Equal(2, decodedAnimation.SoundSettings[0].Length);
            Assert.Equal(12345, decodedAnimation.SoundSettings[0][0]);
            Assert.Equal(100, decodedAnimation.SoundSettings[0][1]);
            Assert.NotNull(decodedAnimation.SoundSettings[1]);
            Assert.Equal(1, decodedAnimation.SoundSettings[1].Length);
            Assert.Equal(54321, decodedAnimation.SoundSettings[1][0]);
        }

        [Fact]
        public void Decode_WithOpcode249_ShouldDecodeExtraData()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(249); // Opcode
            stream.WriteByte(2); // Dictionary entry count
            // Entry 1 (string)
            stream.WriteByte(1); // Is string
            stream.WriteMedInt(1); // Key
            stream.WriteString("TestValue");
            // Entry 2 (int)
            stream.WriteByte(0); // Is not string
            stream.WriteMedInt(2); // Key
            stream.WriteInt(98765);
            stream.WriteByte(0); // End of data
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.NotNull(decodedAnimation.ExtraData);
            Assert.Equal(2, decodedAnimation.ExtraData.Count);
            Assert.True(decodedAnimation.ExtraData.ContainsKey(1));
            Assert.Equal("TestValue", decodedAnimation.ExtraData[1]);
            Assert.True(decodedAnimation.ExtraData.ContainsKey(2));
            Assert.Equal(98765, decodedAnimation.ExtraData[2]);
        }

        [Fact]
        public void Decode_WithOpcode19Before13_ThrowsInvalidDataException()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(19); // Opcode 19
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.Position = 0;

            // Act & Assert
            Assert.Throws<InvalidDataException>(() => codec.Decode(1, stream));
        }

        [Fact]
        public void Decode_WithOpcode3_ShouldDecodeBooleanArray()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(3); // Opcode
            stream.WriteByte(3); // Count
            stream.WriteByte(10);
            stream.WriteByte(20);
            stream.WriteByte(30);
            stream.WriteByte(0); // End of data
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.NotNull(decodedAnimation.ABooleanArray414);
            Assert.True(decodedAnimation.ABooleanArray414[10]);
            Assert.True(decodedAnimation.ABooleanArray414[20]);
            Assert.True(decodedAnimation.ABooleanArray414[30]);
            Assert.False(decodedAnimation.ABooleanArray414[40]);
        }

        [Fact]
        public void Decode_WithOpcode12_ShouldDecodeInterfaceFrames()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(12); // Opcode
            stream.WriteByte(2);  // Count
            // Frames (low)
            stream.WriteShort(100);
            stream.WriteShort(200);
            // Frames (high)
            stream.WriteShort(5);
            stream.WriteShort(6);
            stream.WriteByte(0); // End of data
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.NotNull(decodedAnimation.InterfaceFrames);
            Assert.Equal(2, decodedAnimation.InterfaceFrames.Length);
            Assert.Equal((5 << 16) | 100, decodedAnimation.InterfaceFrames[0]);
            Assert.Equal((6 << 16) | 200, decodedAnimation.InterfaceFrames[1]);
        }

        [Theory]
        [InlineData(14, "ABoolean413")]
        [InlineData(15, "IsTweened")]
        [InlineData(18, "ABoolean409")]
        public void Decode_BooleanOpcodes_ShouldSetCorrectProperties(int opcode, string propertyName)
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte((byte)opcode);
            stream.WriteByte(0);
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);
            var property = typeof(AnimationType).GetProperty(propertyName);

            // Assert
            Assert.NotNull(property);
            Assert.True((bool)property.GetValue(decodedAnimation));
        }

        [Fact]
        public void Decode_WithOpcode20_ShouldDecodeSoundSpeed()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            // Opcode 13 must appear before 20
            stream.WriteByte(13);
            stream.WriteShort(1);
            stream.WriteByte(1);
            stream.WriteMedInt(123);

            stream.WriteByte(20); // Opcode
            stream.WriteByte(0); // Index
            stream.WriteShort(100); // Min speed
            stream.WriteShort(200); // Max speed
            stream.WriteByte(0); // End of data
            stream.Position = 0;

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.NotNull(decodedAnimation.MinSoundSpeed);
            Assert.NotNull(decodedAnimation.MaxSoundSpeed);
            Assert.Equal(100, decodedAnimation.MinSoundSpeed[0]);
            Assert.Equal(200, decodedAnimation.MaxSoundSpeed[0]);
        }

        [Fact]
        public void Decode_WithOpcode22_ShouldBeIgnored()
        {
            // Arrange
            var codec = new AnimationTypeCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(22);
            stream.WriteByte(123); // some dummy data
            stream.WriteByte(0);
            stream.Position = 0;
            var defaultAnimation = new AnimationType(1);

            // Act
            var decodedAnimation = (AnimationType)codec.Decode(1, stream);

            // Assert
            Assert.Equal(defaultAnimation.Id, decodedAnimation.Id);
            Assert.Equal(defaultAnimation.Loop, decodedAnimation.Loop);
        }
    }
}