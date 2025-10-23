using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class GraphicTypeCodecTests
    {
        [Fact]
        public void RoundTrip_AllProperties_ShouldSucceed()
        {
            // Arrange
            var original = new GraphicType(123)
            {
                Contrast = 1,
                Ambient = 2,
                ResizeX = 3,
                Rotation = 4,
                RecolorToFind = new short[] { 5, 6 },
                AByte260 = 2,
                AnimationID = 9,
                AnInt265 = 2560,
                RetextureToReplace = new short[] { 11, 12 },
                ABoolean267 = true,
                RetextureToFind = new short[] { 13, 14 },
                DefaultModelID = 15,
                RecolorToReplace = new short[] { 16, 17 },
                ResizeY = 18,
                AByteArray4428 = new byte[] { 0, 1 },
                AByteArray4433 = new byte[] { 0, 1 }
            };
            var codec = new GraphicTypeCodec();

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = (GraphicType)codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(original.Contrast, decoded.Contrast);
            Assert.Equal(original.Ambient, decoded.Ambient);
            Assert.Equal(original.ResizeX, decoded.ResizeX);
            Assert.Equal(original.Rotation, decoded.Rotation);
            Assert.Equal(original.RecolorToFind, decoded.RecolorToFind);
            Assert.Equal(original.AByte260, decoded.AByte260);
            Assert.Equal(original.AnInt262, decoded.AnInt262);
            Assert.Equal(original.AnimationID, decoded.AnimationID);
            Assert.Equal(original.AnInt265, decoded.AnInt265);
            Assert.Equal(original.RetextureToReplace, decoded.RetextureToReplace);
            Assert.Equal(original.ABoolean267, decoded.ABoolean267);
            Assert.Equal(original.RetextureToFind, decoded.RetextureToFind);
            Assert.Equal(original.DefaultModelID, decoded.DefaultModelID);
            Assert.Equal(original.RecolorToReplace, decoded.RecolorToReplace);
            Assert.Equal(original.ResizeY, decoded.ResizeY);
            Assert.Equal(original.AByteArray4428, decoded.AByteArray4428);
            Assert.Equal(original.AByteArray4433, decoded.AByteArray4433);
        }

        [Fact]
        public void RoundTrip_Opcode16_ShouldSucceed()
        {
            // Arrange
            var original = new GraphicType(123)
            {
                AByte260 = 3,
                AnInt265 = 65536,
            };
            var codec = new GraphicTypeCodec();

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = (GraphicType)codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.AByte260, decoded.AByte260);
            Assert.Equal(original.AnInt265, decoded.AnInt265);
        }

        [Fact]
        public void RoundTrip_Opcode15_ShouldSucceed()
        {
            // Arrange
            var original = new GraphicType(123)
            {
                AByte260 = 3,
                AnInt265 = 65535,
            };
            var codec = new GraphicTypeCodec();

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = (GraphicType)codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.AByte260, decoded.AByte260);
            Assert.Equal(original.AnInt265, decoded.AnInt265);
        }

        [Fact]
        public void RoundTrip_Opcode9_ShouldSucceed()
        {
            // Arrange
            var original = new GraphicType(123)
            {
                AByte260 = 3,
                AnInt265 = 8224,
            };
            var codec = new GraphicTypeCodec();

            // Act
            var encodedStream = codec.Encode(original);
            encodedStream.Position = 0;
            var decoded = (GraphicType)codec.Decode(original.Id, encodedStream);

            // Assert
            Assert.Equal(original.AByte260, decoded.AByte260);
            Assert.Equal(original.AnInt265, decoded.AnInt265);
        }
    }
}