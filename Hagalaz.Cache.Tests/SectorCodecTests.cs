using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;
using Hagalaz.Cache;

namespace Hagalaz.Cache.Tests
{
    public class SectorCodecTests
    {
        [Fact]
        public void TestStandardSectorCodec()
        {
            // Arrange
            var codec = new SectorCodec();
            ISector originalSector = new Sector(1, 2, 3, 4);
            var dataBlock = new byte[Sector.DataBlockSize];

            // Act
            var encodedData = codec.Encode(originalSector, dataBlock);
            var decodedSector = codec.Decode(encodedData, false);

            // Assert
            Assert.Equal(originalSector.FileID, decodedSector.FileID);
            Assert.Equal(originalSector.ChunkID, decodedSector.ChunkID);
            Assert.Equal(originalSector.NextSectorID, decodedSector.NextSectorID);
            Assert.Equal(originalSector.IndexID, decodedSector.IndexID);
        }

        [Fact]
        public void TestExtendedSectorCodec()
        {
            // Arrange
            var codec = new SectorCodec();
            ISector originalSector = new Sector(ushort.MaxValue + 1, 2, 3, 4);
            var dataBlock = new byte[Sector.ExtendedDataBlockSize];

            // Act
            var encodedData = codec.Encode(originalSector, dataBlock);
            var decodedSector = codec.Decode(encodedData, true);

            // Assert
            Assert.Equal(originalSector.FileID, decodedSector.FileID);
            Assert.Equal(originalSector.ChunkID, decodedSector.ChunkID);
            Assert.Equal(originalSector.NextSectorID, decodedSector.NextSectorID);
            Assert.Equal(originalSector.IndexID, decodedSector.IndexID);
        }
    }
}