using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ArchiveDecoderTests
    {
        [Fact]
        public void Decode_SingleFile_ShouldReturnArchiveWithOneEntry()
        {
            // Arrange
            var decoder = new ArchiveDecoder();
            var fileData = new byte[] { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(fileData);
            var container = new Container(CompressionType.None, stream, -1);

            // Act
            using var archive = decoder.Decode(container, 1);

            // Assert
            Assert.NotNull(archive);
            Assert.NotNull(archive.Entries);
            Assert.Single(archive.Entries);
            var entry = archive.GetEntry(0);
            Assert.Equal(fileData.Length, entry.Length);
            var entryData = new byte[entry.Length];
            entry.Read(entryData, 0, entryData.Length);
            Assert.Equal(fileData, entryData);
        }

        [Fact]
        public void Decode_MultipleFiles_ShouldReturnArchiveWithMultipleEntries()
        {
            // Arrange
            var decoder = new ArchiveDecoder();

            // Manually create a stream with 2 files and 1 chunk
            using var stream = new MemoryStream();
            // File 1 data
            stream.Write(new byte[] { 1, 2, 3 });
            // File 2 data
            stream.Write(new byte[] { 4, 5, 6, 7 });

            // Chunk sizes (delta-encoded)
            // File 1, chunk 1 size
            var chunkSize1 = System.BitConverter.GetBytes(3);
            if (System.BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(chunkSize1);
            }
            stream.Write(chunkSize1);
            // File 2, chunk 1 size
            var chunkSize2 = System.BitConverter.GetBytes(4);
            if (System.BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(chunkSize2);
            }
            stream.Write(chunkSize2);

            // Number of chunks
            stream.WriteByte(1);

            stream.Position = 0;
            var container = new Container(CompressionType.None, stream, -1);

            // Act
            using var archive = decoder.Decode(container, 2);

            // Assert
            Assert.NotNull(archive);
            Assert.NotNull(archive.Entries);
            Assert.Equal(2, archive.Entries.Length);

            // Check file 1
            var entry1 = archive.GetEntry(0);
            Assert.Equal(3, entry1.Length);
            var entry1Data = new byte[entry1.Length];
            entry1.Read(entry1Data, 0, entry1Data.Length);
            Assert.Equal(new byte[] { 1, 2, 3 }, entry1Data);

            // Check file 2
            var entry2 = archive.GetEntry(1);
            Assert.Equal(4, entry2.Length);
            var entry2Data = new byte[entry2.Length];
            entry2.Read(entry2Data, 0, entry2Data.Length);
            Assert.Equal(new byte[] { 4, 5, 6, 7 }, entry2Data);
        }
    }
}
