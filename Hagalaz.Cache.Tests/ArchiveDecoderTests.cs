using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
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

            // Chunk sizes are delta-encoded. The cumulative sizes are 3 and 4.
            stream.WriteInt(3);
            stream.WriteInt(1);

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

        [Fact]
        public void Decode_NegativeFooterDelta_UsesCumulativeChunkLengths()
        {
            var decoder = new ArchiveDecoder();
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7 });
            stream.Write(new byte[] { 8, 9, 10 });
            stream.WriteInt(7);
            stream.WriteInt(-4);
            stream.WriteByte(1);
            stream.Position = 0;

            using var archive = decoder.Decode(new Container(CompressionType.None, stream, -1), 2);

            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, archive.GetEntry(0).ToArray());
            Assert.Equal(new byte[] { 8, 9, 10 }, archive.GetEntry(1).ToArray());
        }

        [Fact]
        public void Decode_MultipleChunks_ReassemblesEachEntryInChunkOrder()
        {
            var decoder = new ArchiveDecoder();
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 1, 2 });
            stream.Write(new byte[] { 3, 4, 5 });
            stream.Write(new byte[] { 6 });
            stream.Write(new byte[] { 7, 8, 9, 10 });
            stream.WriteInt(2);
            stream.WriteInt(1);
            stream.WriteInt(1);
            stream.WriteInt(3);
            stream.WriteByte(2);
            stream.Position = 0;

            using var archive = decoder.Decode(new Container(CompressionType.None, stream, -1), 2);

            Assert.Equal(new byte[] { 1, 2, 6 }, archive.GetEntry(0).ToArray());
            Assert.Equal(new byte[] { 3, 4, 5, 7, 8, 9, 10 }, archive.GetEntry(1).ToArray());
        }

        [Fact]
        public void Decode_NegativeCumulativeChunkSize_ThrowsInvalidDataException()
        {
            var decoder = new ArchiveDecoder();
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 1, 2 });
            stream.WriteInt(2);
            stream.WriteInt(-3);
            stream.WriteByte(1);
            stream.Position = 0;

            var exception = Assert.Throws<InvalidDataException>(() =>
                decoder.Decode(new Container(CompressionType.None, stream, -1), 2));

            Assert.Contains("negative chunk size", exception.Message);
        }
    }
}
