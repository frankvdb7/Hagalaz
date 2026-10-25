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
            table.AddEntry(1_000_000, CreateEntry(1, "high"));

            Assert.Equal(2, table.Count);
            IReferenceTable referenceTable = table;
            Assert.Equal(2, referenceTable.Count);
            Assert.Equal(1_000_001, table.Capacity);
            Assert.Equal(1_000_000, table.GetFileId("high"));
            Assert.Null(table.GetEntry(50));

            var copy = new ReferenceTable(table);
            Assert.Equal(2, copy.Count);
            Assert.Equal(1_000_001, copy.Capacity);
            Assert.NotNull(copy.GetEntry(1_000_000));

            var codec = new ReferenceTableCodec();
            using var encoded = codec.Encode(table);
            var allocatedBeforeDecode = System.GC.GetAllocatedBytesForCurrentThread();
            var decoded = codec.Decode(encoded);
            var allocatedDuringDecode = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBeforeDecode;

            Assert.True(allocatedDuringDecode < 1_000_000, $"Sparse archive IDs allocated {allocatedDuringDecode} bytes while decoding.");
            Assert.Equal(2, decoded.Count);
            Assert.Equal(1_000_001, decoded.Capacity);
            Assert.NotNull(decoded.GetEntry(1));
            Assert.Equal(0, ((ReferenceTableEntry)decoded.GetEntry(1)!).Index);
            Assert.NotNull(decoded.GetEntry(1_000_000));
            Assert.Equal(1, ((ReferenceTableEntry)decoded.GetEntry(1_000_000)!).Index);
            Assert.Equal(0, decoded.GetEntry(1_000_000)!.GetEntry(0)!.Index);
            Assert.Null(decoded.GetEntry(50));
        }

        [Theory]
        [InlineData((byte)5, 0)]
        [InlineData((byte)6, 42)]
        [InlineData((byte)7, 42)]
        public void EncodeDecode_PreservesLargeSparseArchiveAndChildIds(byte protocol, int version)
        {
            const int largeId = 40000;
            var table = new ReferenceTable(protocol, version, (ReferenceTableFlags)0, 1);
            var archive = new ReferenceTableEntry(0);
            archive.InitializeEntries(1);
            archive.AddEntry(largeId, new ReferenceTableChildEntry(0));
            table.AddEntry(largeId, archive);
            var codec = new ReferenceTableCodec();

            using var encoded = codec.Encode(table);
            var decoded = codec.Decode(encoded);

            Assert.Equal(protocol, decoded.Protocol);
            Assert.Equal(version, decoded.Version);
            Assert.NotNull(decoded.GetEntry(largeId));
            Assert.NotNull(decoded.GetEntry(largeId)?.GetEntry(largeId));
        }

        [Theory]
        [InlineData((byte)5, 0)]
        [InlineData((byte)6, 42)]
        public void EncodeDecode_PreservesCountsAtBigSmartThreshold(byte protocol, int version)
        {
            const int threshold = short.MaxValue;
            var table = new ReferenceTable(protocol, version, (ReferenceTableFlags)0, threshold);

            for (var archiveId = 0; archiveId < threshold; archiveId++)
            {
                var memberCount = archiveId == threshold - 1 ? threshold : 0;
                var archive = new ReferenceTableEntry(archiveId);
                archive.InitializeEntries(memberCount);
                for (var memberId = 0; memberId < memberCount; memberId++)
                {
                    archive.AddEntry(memberId, new ReferenceTableChildEntry(memberId));
                }

                table.AddEntry(archiveId, archive);
            }

            var codec = new ReferenceTableCodec();
            using var encoded = codec.Encode(table);
            var decoded = codec.Decode(encoded);

            Assert.Equal(threshold, decoded.Count);
            var lastArchive = decoded.GetEntry(threshold - 1);
            Assert.NotNull(lastArchive);
            Assert.Equal(threshold, lastArchive!.Capacity);
        }

        [Fact]
        public void Encode_RejectsLegacyProtocolIdsOutsideUnsignedShortRange()
        {
            var table = new ReferenceTable(6, 42, (ReferenceTableFlags)0, 1);
            var archive = new ReferenceTableEntry(0);
            archive.InitializeEntries(0);
            table.AddEntry(ushort.MaxValue + 1, archive);

            Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceTableCodec().Encode(table));
        }

        [Theory]
        [InlineData((byte)4)]
        [InlineData((byte)8)]
        public void Encode_RejectsProtocolsNotSupportedByDecoder(byte protocol)
        {
            var table = new ReferenceTable(protocol, 42, (ReferenceTableFlags)0, 0);

            Assert.Throws<System.IO.InvalidDataException>(() => new ReferenceTableCodec().Encode(table));
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
