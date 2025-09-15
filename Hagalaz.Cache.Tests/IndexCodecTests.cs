using Hagalaz.Cache;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class IndexCodecTests
    {
        private readonly IIndexCodec _codec;

        public IndexCodecTests()
        {
            _codec = new IndexCodec();
        }

        [Fact]
        public void Decode_ValidData_ReturnsCorrectIndex()
        {
            // Arrange
            var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
            var expectedSize = 258; // 0x000102
            var expectedSector = 197637; // 0x030405

            // Act
            var index = _codec.Decode(data);

            // Assert
            Assert.Equal(expectedSize, index.Size);
            Assert.Equal(expectedSector, index.SectorID);
        }

        [Fact]
        public void Encode_ValidIndex_ReturnsCorrectData()
        {
            // Arrange
            var index = new Index(258, 197637);

            // Act
            var data = _codec.Encode(index);

            // Assert
            Assert.Equal(6, data.Length);
            Assert.Equal(0x00, data[0]);
            Assert.Equal(0x01, data[1]);
            Assert.Equal(0x02, data[2]);
            Assert.Equal(0x03, data[3]);
            Assert.Equal(0x04, data[4]);
            Assert.Equal(0x05, data[5]);
        }

        [Fact]
        public void EncodeDecode_Symmetry()
        {
            // Arrange
            var originalIndex = new Index(12345, 67890);

            // Act
            var encoded = _codec.Encode(originalIndex);
            var decodedIndex = _codec.Decode(encoded);

            // Assert
            Assert.Equal(originalIndex.Size, decodedIndex.Size);
            Assert.Equal(originalIndex.SectorID, decodedIndex.SectorID);
        }
    }
}
