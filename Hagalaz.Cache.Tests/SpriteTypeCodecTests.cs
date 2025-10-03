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
    }
}