using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class SpriteTypeCodecTests
    {
        [Fact]
        public void Encode_Decode_RoundTrip_ShouldRestoreSameData()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            var originalSprite = new SpriteType(1)
            {
                Image = new Image<Rgba32>(1, 1)
            };
            originalSprite.Image[0, 0] = new Rgba32(255, 0, 0);

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            // Assert
            Assert.Equal(originalSprite.Image[0,0], decodedSprite.Image.Frames[0][0, 0]);
        }

        [Fact]
        public void Encode_Decode_MultiFrame_ShouldRestoreAllFrames()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            var originalSprite = new SpriteType(1)
            {
                Image = new Image<Rgba32>(2, 1)
            };

            originalSprite.Image.Frames.RootFrame[0, 0] = new Rgba32(255, 0, 0);
            originalSprite.Image.Frames.RootFrame[1, 0] = new Rgba32(0, 255, 0);

            var secondImage = new Image<Rgba32>(2, 1);
            secondImage.Frames.RootFrame[0, 0] = new Rgba32(0, 0, 255);
            secondImage.Frames.RootFrame[1, 0] = new Rgba32(255, 255, 0);
            originalSprite.Image.Frames.AddFrame(secondImage.Frames.RootFrame);

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            // Assert
            Assert.Equal(originalSprite.Image.Frames.Count, decodedSprite.Image.Frames.Count);

            Assert.Equal(originalSprite.Image.Frames[0][0,0], decodedSprite.Image.Frames[0][0,0]);
            Assert.Equal(originalSprite.Image.Frames[0][1,0], decodedSprite.Image.Frames[0][1,0]);
            Assert.Equal(originalSprite.Image.Frames[1][0,0], decodedSprite.Image.Frames[1][0,0]);
            Assert.Equal(originalSprite.Image.Frames[1][1,0], decodedSprite.Image.Frames[1][1,0]);
        }

        [Fact]
        public void Encode_Decode_WithAlpha_ShouldRestoreAlpha()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            var originalSprite = new SpriteType(1)
            {
                Image = new Image<Rgba32>(1, 1)
            };
            originalSprite.Image[0, 0] = new Rgba32(255, 0, 0, 128);

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            // Assert
            Assert.Equal(originalSprite.Image[0,0], decodedSprite.Image.Frames[0][0,0]);
            Assert.Equal(128, decodedSprite.Image.Frames[0][0,0].A);
        }

        [Fact]
        public void Encode_Decode_DifferentDimensions_ShouldRestoreCorrectly()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            var originalSprite = new SpriteType(1)
            {
                Image = new Image<Rgba32>(10, 20)
            };
            originalSprite.Image[5, 10] = new Rgba32(128, 0, 128);

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            // Assert
            Assert.Equal(originalSprite.Image.Width, decodedSprite.Image.Width);
            Assert.Equal(originalSprite.Image.Height, decodedSprite.Image.Height);

            Assert.Equal(originalSprite.Image[5,10], decodedSprite.Image.Frames[0][5,10]);
        }
    }
}