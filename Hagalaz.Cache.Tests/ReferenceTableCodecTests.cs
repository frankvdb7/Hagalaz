using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ReferenceTableCodecTests
    {
        [Fact]
        public void TestEncodeDecode()
        {
            // Arrange
            var codec = new ReferenceTableCodec();
            var table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, 1);
            var entry = new ReferenceTableEntry(0);
            entry.InitializeEntries(1);
            entry.Id = 1;
            entry.Crc32 = 0x12345678;
            entry.Version = 2;
            var childEntry = new ReferenceTableChildEntry(0)
            {
                Id = 3
            };
            entry.AddEntry(0, childEntry);
            table.AddEntry(0, entry);

            // Act
            var encoded = codec.Encode(table);
            var decoded = codec.Decode(encoded);

            // Assert
            Assert.Equal(table.Protocol, decoded.Protocol);
            Assert.Equal(table.Version, decoded.Version);
            Assert.Equal(table.Flags, decoded.Flags);
            Assert.Equal(table.Capacity, decoded.Capacity);

            var originalEntry = table.GetEntry(0);
            var decodedEntry = decoded.GetEntry(0);

            Assert.NotNull(originalEntry);
            Assert.NotNull(decodedEntry);

            if (originalEntry is null || decodedEntry is null)
            {
                return;
            }

            Assert.Equal(originalEntry.Id, decodedEntry.Id);
            Assert.Equal(originalEntry.Crc32, decodedEntry.Crc32);
            Assert.Equal(originalEntry.Version, decodedEntry.Version);
            Assert.Equal(originalEntry.Capacity, decodedEntry.Capacity);

            var originalChildEntry = originalEntry.GetEntry(0);
            var decodedChildEntry = decodedEntry.GetEntry(0);

            Assert.NotNull(originalChildEntry);
            Assert.NotNull(decodedChildEntry);

            if (originalChildEntry is null || decodedChildEntry is null)
            {
                return;
            }

            Assert.Equal(originalChildEntry.Id, decodedChildEntry.Id);
        }

        [Fact]
        public void SparseArchiveIds_PreserveCapacityCountLookupAndRoundTrip()
        {
            var table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, 2);
            table.AddEntry(1, CreateEntry(0, "low"));
            table.AddEntry(100, CreateEntry(1, "high"));

            Assert.Equal(2, table.Count);
            Assert.Equal(101, table.Capacity);
            Assert.Equal(100, table.GetFileId("high"));
            Assert.Null(table.GetEntry(50));

            var copy = new ReferenceTable(table);
            Assert.Equal(2, copy.Count);
            Assert.Equal(101, copy.Capacity);
            Assert.NotNull(copy.GetEntry(100));

            var codec = new ReferenceTableCodec();
            using var encoded = codec.Encode(table);
            var decoded = codec.Decode(encoded);

            Assert.Equal(2, decoded.Count);
            Assert.Equal(101, decoded.Capacity);
            Assert.NotNull(decoded.GetEntry(1));
            Assert.NotNull(decoded.GetEntry(100));
            Assert.Null(decoded.GetEntry(50));
        }

        private static ReferenceTableEntry CreateEntry(int index, string name)
        {
            var entry = new ReferenceTableEntry(index)
            {
                Id = ReferenceTable.CalculateId(name)
            };
            entry.InitializeEntries(1);
            entry.AddEntry(0, new ReferenceTableChildEntry(0));
            return entry;
        }
    }
}
