using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
            using var originalSprite = new SpriteType(1, 1, 1);
            originalSprite.Image[0, 0] = new Rgba32(255, 0, 0);

            // Act
            using var decodedSprite = RoundTrip(codec, originalSprite);

            // Assert
            Assert.Equal(originalSprite.Image[0,0], decodedSprite.Image.Frames[0][0, 0]);
        }

        [Fact]
        public void Encode_Decode_MultiFrame_ShouldRestoreAllFrames()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 2, 1);

            originalSprite.Image.Frames.RootFrame[0, 0] = new Rgba32(255, 0, 0);
            originalSprite.Image.Frames.RootFrame[1, 0] = new Rgba32(0, 255, 0);

            using var secondImage = new Image<Rgba32>(2, 1);
            secondImage.Frames.RootFrame[0, 0] = new Rgba32(0, 0, 255);
            secondImage.Frames.RootFrame[1, 0] = new Rgba32(255, 255, 0);
            originalSprite.Image.Frames.AddFrame(secondImage.Frames.RootFrame);

            // Act
            using var decodedSprite = RoundTrip(codec, originalSprite);

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
            using var originalSprite = new SpriteType(1, 1, 1);
            originalSprite.Image[0, 0] = new Rgba32(255, 0, 0, 128);

            // Act
            using var decodedSprite = RoundTrip(codec, originalSprite);

            // Assert
            Assert.Equal(originalSprite.Image[0,0], decodedSprite.Image.Frames[0][0,0]);
            Assert.Equal(128, decodedSprite.Image.Frames[0][0,0].A);
        }

        [Fact]
        public void Encode_Decode_FullyTransparentRgbWithoutAlphaPlane_ShouldRemainTransparent()
        {
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 3, 1);
            originalSprite.Image[0, 0] = new Rgba32(255, 0, 0, 255);
            originalSprite.Image[1, 0] = new Rgba32(255, 0, 255, 0);
            originalSprite.Image[2, 0] = new Rgba32(0, 255, 0, 255);

            using var decodedSprite = RoundTrip(codec, originalSprite);

            Assert.Equal(new Rgba32(0, 0, 0, 0), decodedSprite.Image.Frames[0][1, 0]);
        }

        [Fact]
        public void Encode_Decode_AnimatedFramesWithTransparentRgbAndAlphaVariants_ShouldPreserveVisiblePixels()
        {
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 257, 1);
            using var secondFrame = new Image<Rgba32>(257, 1);

            for (var x = 0; x < 257; x++)
            {
                originalSprite.Image[x, 0] = x switch
                {
                    0 => new Rgba32(20, 40, 60, 255),
                    1 => new Rgba32(255, 0, 255, 0),
                    _ => new Rgba32(20, 40, 60, (byte)(x - 1))
                };

                secondFrame[x, 0] = x switch
                {
                    0 => new Rgba32(20, 40, 60, 255),
                    1 => new Rgba32(255, 0, 255, 0),
                    _ => new Rgba32(20, 40, 60, (byte)(257 - x))
                };
            }

            originalSprite.Image.Frames.AddFrame(secondFrame.Frames.RootFrame);

            using var decodedSprite = RoundTrip(codec, originalSprite);

            Assert.Equal(2, decodedSprite.Image.Frames.Count);
            for (var frameIndex = 0; frameIndex < originalSprite.Image.Frames.Count; frameIndex++)
            {
                for (var x = 0; x < originalSprite.Image.Width; x++)
                {
                    var expected = originalSprite.Image.Frames[frameIndex][x, 0];
                    var actual = decodedSprite.Image.Frames[frameIndex][x, 0];

                    Assert.Equal(expected.A, actual.A);
                    if (expected.A == 0)
                    {
                        Assert.Equal(new Rgba32(0, 0, 0, 0), actual);
                    }
                    else
                    {
                        Assert.Equal(expected.R, actual.R);
                        Assert.Equal(expected.G, actual.G);
                        Assert.Equal(expected.B, actual.B);
                    }
                }
            }
        }

        [Fact]
        public void Encode_Decode_DifferentDimensions_ShouldRestoreCorrectly()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 10, 20);
            originalSprite.Image[5, 10] = new Rgba32(128, 0, 128);

            // Act
            using var decodedSprite = RoundTrip(codec, originalSprite);

            // Assert
            Assert.Equal(originalSprite.Image.Width, decodedSprite.Image.Width);
            Assert.Equal(originalSprite.Image.Height, decodedSprite.Image.Height);

            Assert.Equal(originalSprite.Image[5,10], decodedSprite.Image.Frames[0][5,10]);
        }

        [Fact]
        public void Encode_Decode_WithOffset_ShouldRestoreCorrectly()
        {
            // Arrange
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 20, 20);
            // Create a 2x2 red square at offset (5, 8)
            originalSprite.Image[5, 8] = new Rgba32(255, 0, 0);
            originalSprite.Image[6, 8] = new Rgba32(255, 0, 0);
            originalSprite.Image[5, 9] = new Rgba32(255, 0, 0);
            originalSprite.Image[6, 9] = new Rgba32(255, 0, 0);

            // Act
            using var decodedSprite = RoundTrip(codec, originalSprite);

            // Assert
            Assert.Equal(originalSprite.Image.Width, decodedSprite.Image.Width);
            Assert.Equal(originalSprite.Image.Height, decodedSprite.Image.Height);

            // Check the pixels of the red square
            Assert.Equal(originalSprite.Image[5, 8], decodedSprite.Image.Frames[0][5, 8]);
            Assert.Equal(originalSprite.Image[6, 8], decodedSprite.Image.Frames[0][6, 8]);
            Assert.Equal(originalSprite.Image[5, 9], decodedSprite.Image.Frames[0][5, 9]);
            Assert.Equal(originalSprite.Image[6, 9], decodedSprite.Image.Frames[0][6, 9]);

            // Check a pixel outside the square to ensure it's transparent
            Assert.Equal(new Rgba32(0,0,0,0), decodedSprite.Image.Frames[0][0, 0]);
        }

        [Fact]
        public void Decode_InvalidPaletteIndex_Throws()
        {
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 1, 1);
            originalSprite.Image[0, 0] = new Rgba32(255, 0, 0);
            using var encoded = codec.Encode(originalSprite);
            var corruptData = encoded.ToArray();
            corruptData[1] = byte.MaxValue;
            using var corruptStream = new MemoryStream(corruptData);

            Assert.Throws<IndexOutOfRangeException>(() => codec.Decode(1, corruptStream));
        }

        [Fact]
        public void Decode_HorizontalAlphaPixels_PreservesPixelOrder()
        {
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 2, 2);
            originalSprite.Image[0, 0] = new Rgba32(255, 0, 0, 64);
            originalSprite.Image[0, 1] = new Rgba32(0, 255, 0, 128);
            originalSprite.Image[1, 0] = new Rgba32(0, 0, 255, 192);
            originalSprite.Image[1, 1] = new Rgba32(255, 255, 0, 255);

            using var encoded = codec.Encode(originalSprite);
            var horizontalData = encoded.ToArray();
            horizontalData[0] = (byte)SpriteType.Flags.Alpha;
            (horizontalData[2], horizontalData[3]) = (horizontalData[3], horizontalData[2]);
            (horizontalData[6], horizontalData[7]) = (horizontalData[7], horizontalData[6]);
            using var horizontalStream = new MemoryStream(horizontalData);

            using var decodedSprite = (SpriteType)codec.Decode(originalSprite.Id, horizontalStream);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    Assert.Equal(originalSprite.Image[x, y], decodedSprite.Image.Frames[0][x, y]);
                }
            }
        }

        [Fact]
        public void Encode_TooManyColorsAcrossFrames_ThrowsArgumentException()
        {
            var codec = new SpriteTypeCodec();
            using var sprite = new SpriteType(1, 128, 1);
            using var secondFrame = new Image<Rgba32>(128, 1);

            for (var x = 0; x < 128; x++)
            {
                sprite.Image[x, 0] = new Rgba32((byte)x, 0, 0, 255);
                secondFrame[x, 0] = new Rgba32((byte)x, 1, 0, 255);
            }

            sprite.Image.Frames.AddFrame(secondFrame.Frames.RootFrame);

            var exception = Assert.Throws<ArgumentException>(() => codec.Encode(sprite));

            Assert.Contains("255 visible RGB colors", exception.Message);
        }

        [Fact]
        public void Encode_Decode_MaximumPaletteColorCount_ShouldSucceed()
        {
            var codec = new SpriteTypeCodec();
            using var originalSprite = new SpriteType(1, 255, 1);

            for (var x = 0; x < 255; x++)
            {
                originalSprite.Image[x, 0] = new Rgba32(10, 0, (byte)x);
            }

            using var decodedSprite = RoundTrip(codec, originalSprite);

            for (var x = 0; x < 255; x++)
            {
                Assert.Equal(originalSprite.Image[x, 0], decodedSprite.Image.Frames[0][x, 0]);
            }
        }

        private static SpriteType RoundTrip(SpriteTypeCodec codec, SpriteType originalSprite)
        {
            using var encodedStream = codec.Encode(originalSprite);
            encodedStream.Position = 0;
            return (SpriteType)codec.Decode(originalSprite.Id, encodedStream);
        }
    }
}
