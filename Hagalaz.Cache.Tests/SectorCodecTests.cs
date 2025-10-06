using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;
using Hagalaz.Cache;
using System;
using System.Linq;

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
            new Random().NextBytes(dataBlock);

            // Act
            var encodedData = codec.Encode(originalSector, dataBlock);
            var decodedSector = codec.Decode(encodedData, false);
            var decodedDataBlock = encodedData.Skip(Sector.DataHeaderSize).ToArray();

            // Assert
            Assert.Equal(originalSector.FileID, decodedSector.FileID);
            Assert.Equal(originalSector.ChunkID, decodedSector.ChunkID);
            Assert.Equal(originalSector.NextSectorID, decodedSector.NextSectorID);
            Assert.Equal(originalSector.IndexID, decodedSector.IndexID);
            Assert.Equal(dataBlock, decodedDataBlock);
        }

        [Fact]
        public void TestExtendedSectorCodec()
        {
            // Arrange
            var codec = new SectorCodec();
            ISector originalSector = new Sector(ushort.MaxValue + 1, 2, 3, 4);
            var dataBlock = new byte[Sector.ExtendedDataBlockSize];
            new Random().NextBytes(dataBlock);

            // Act
            var encodedData = codec.Encode(originalSector, dataBlock);
            var decodedSector = codec.Decode(encodedData, true);
            var decodedDataBlock = encodedData.Skip(Sector.ExtendedDataHeaderSize).ToArray();

            // Assert
            Assert.Equal(originalSector.FileID, decodedSector.FileID);
            Assert.Equal(originalSector.ChunkID, decodedSector.ChunkID);
            Assert.Equal(originalSector.NextSectorID, decodedSector.NextSectorID);
            Assert.Equal(originalSector.IndexID, decodedSector.IndexID);
            Assert.Equal(dataBlock, decodedDataBlock);
        }
    }
}