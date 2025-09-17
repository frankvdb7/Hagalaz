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
    }
}
