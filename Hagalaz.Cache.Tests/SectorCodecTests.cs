using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;
using Hagalaz.Cache.Models;

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
            Assert.Equal(Sector.DataSize, encodedData.Length);
        }

        [Fact]
        public void Encode_PartialDataBlock_PadsToFullSectorSize()
        {
            // Arrange
            var codec = new SectorCodec();
            ISector originalSector = new Sector(1, 1, 0, 1);
            var partialData = new byte[100];
            new Random().NextBytes(partialData);

            // Act
            var encodedData = codec.Encode(originalSector, partialData);

            // Assert
            Assert.Equal(Sector.DataSize, encodedData.Length);

            // Verify data was written correctly
            var writtenData = encodedData.AsSpan(Sector.DataHeaderSize, 100).ToArray();
            Assert.Equal(partialData, writtenData);

            // Verify padding is zero
            for (int i = Sector.DataHeaderSize + 100; i < Sector.DataSize; i++)
            {
                Assert.Equal(0, encodedData[i]);
            }
        }

        [Fact]
        public void Encode_ReadOnlySpanOverload_WorksCorrectly()
        {
            // Arrange
            var codec = new SectorCodec();
            ISector originalSector = new Sector(1, 1, 0, 1);
            var dataBlock = new byte[Sector.DataBlockSize];
            new Random().NextBytes(dataBlock);
            ReadOnlySpan<byte> span = dataBlock;

            // Act
            var encodedData = codec.Encode(originalSector, span);

            // Assert
            Assert.Equal(Sector.DataSize, encodedData.Length);
            var decodedDataBlock = encodedData.Skip(Sector.DataHeaderSize).ToArray();
            Assert.Equal(dataBlock, decodedDataBlock);
        }
    }
}