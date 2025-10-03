using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using Xunit;
using System.Linq;
using Hagalaz.Cache.Extensions;

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
                Image = new Image<Bgra5551>(1, 1)
            };
            originalSprite.Image[0, 0] = Color.Red.ToPixel<Bgra5551>();

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            var originalRgba = new Rgba32();
            originalSprite.Image[0, 0].ToRgba32(ref originalRgba);

            var decodedRgba = new Rgba32();
            decodedSprite.Image[0, 0].ToRgba32(ref decodedRgba);

            // Assert
            Assert.Equal(originalRgba, decodedRgba);
        }

        [Fact]
        public void Encode_Decode_MultiFrame_ShouldRestoreAllFrames()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            var originalSprite = new SpriteType(1)
            {
                Image = new Image<Bgra5551>(2, 1)
            };

            originalSprite.Image.Frames.RootFrame[0, 0] = Color.Red.ToPixel<Bgra5551>();
            originalSprite.Image.Frames.RootFrame[1, 0] = Color.Green.ToPixel<Bgra5551>();

            var secondFrame = originalSprite.Image.Frames.CloneFrame(0);
            secondFrame[0, 0] = Color.Blue.ToPixel<Bgra5551>();
            secondFrame[1, 0] = Color.Yellow.ToPixel<Bgra5551>();
            originalSprite.Image.Frames.AddFrame(secondFrame);

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            // Assert
            Assert.Equal(originalSprite.Image.Frames.Count, decodedSprite.Image.Frames.Count);

            var originalFrame0_0 = new Rgba32();
            originalSprite.Image.Frames[0][0,0].ToRgba32(ref originalFrame0_0);
            var decodedFrame0_0 = new Rgba32();
            decodedSprite.Image.Frames[0][0,0].ToRgba32(ref decodedFrame0_0);
            Assert.Equal(originalFrame0_0, decodedFrame0_0);

            var originalFrame0_1 = new Rgba32();
            originalSprite.Image.Frames[0][1,0].ToRgba32(ref originalFrame0_1);
            var decodedFrame0_1 = new Rgba32();
            decodedSprite.Image.Frames[0][1,0].ToRgba32(ref decodedFrame0_1);
            Assert.Equal(originalFrame0_1, decodedFrame0_1);

            var originalFrame1_0 = new Rgba32();
            originalSprite.Image.Frames[1][0,0].ToRgba32(ref originalFrame1_0);
            var decodedFrame1_0 = new Rgba32();
            decodedSprite.Image.Frames[1][0,0].ToRgba32(ref decodedFrame1_0);
            Assert.Equal(originalFrame1_0, decodedFrame1_0);

            var originalFrame1_1 = new Rgba32();
            originalSprite.Image.Frames[1][1,0].ToRgba32(ref originalFrame1_1);
            var decodedFrame1_1 = new Rgba32();
            decodedSprite.Image.Frames[1][1,0].ToRgba32(ref decodedFrame1_1);
            Assert.Equal(originalFrame1_1, decodedFrame1_1);
        }

        [Fact]
        public void Encode_Decode_WithAlpha_ShouldRestoreAlpha()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            var originalSprite = new SpriteType(1)
            {
                Image = new Image<Bgra5551>(1, 1)
            };
            var semiTransparentRed = new Color(Color.Red, 0.5f);
            originalSprite.Image[0, 0] = semiTransparentRed.ToPixel<Bgra5551>();

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            // Assert
            var originalRgba = new Rgba32();
            originalSprite.Image[0, 0].ToRgba32(ref originalRgba);

            var decodedRgba = new Rgba32();
            decodedSprite.Image[0, 0].ToRgba32(ref decodedRgba);

            Assert.Equal(originalRgba, decodedRgba);
            Assert.Equal(128, decodedRgba.A);
        }

        [Fact]
        public void Encode_Decode_DifferentDimensions_ShouldRestoreCorrectly()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            var originalSprite = new SpriteType(1)
            {
                Image = new Image<Bgra5551>(10, 20)
            };
            originalSprite.Image[5, 10] = Color.Purple.ToPixel<Bgra5551>();

            // Act
            var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            var decodedSprite = (SpriteType)codec.Decode(encodedStream);

            // Assert
            Assert.Equal(originalSprite.Image.Width, decodedSprite.Image.Width);
            Assert.Equal(originalSprite.Image.Height, decodedSprite.Image.Height);

            var originalRgba = new Rgba32();
            originalSprite.Image[5, 10].ToRgba32(ref originalRgba);

            var decodedRgba = new Rgba32();
            decodedSprite.Image[5, 10].ToRgba32(ref decodedRgba);

            Assert.Equal(originalRgba, decodedRgba);
        }
    }
}