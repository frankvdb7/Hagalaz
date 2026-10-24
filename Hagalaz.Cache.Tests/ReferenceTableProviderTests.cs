using Moq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ReferenceTableProviderTests
    {
        [Fact]
        public void ReadReferenceTable_ShouldReturnReferenceTable()
        {
            // Arrange
            var fileStoreMock = new Mock<IFileStore>();
            var containerDecoderMock = new Mock<IContainerDecoder>();
            var referenceTableCodec = new ReferenceTableCodec();
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
            var encoded = referenceTableCodec.Encode(table);

            var containerMock = new Mock<IContainer>();
            containerMock.Setup(c => c.Data).Returns(encoded);

            fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(255);
            fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
            containerDecoderMock.Setup(cf => cf.Decode(It.IsAny<MemoryStream>())).Returns(containerMock.Object);

            var provider = new ReferenceTableProvider(fileStoreMock.Object, containerDecoderMock.Object, referenceTableCodec);

            // Act
            var result = provider.ReadReferenceTable(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(table.Protocol, result.Protocol);
            Assert.Equal(table.Version, result.Version);
            Assert.Equal(table.Flags, result.Flags);
        }

        [Fact]
        public void Invalidate_CausesReferenceTableToBeReadAgain()
        {
            var fileStoreMock = new Mock<IFileStore>();
            var containerDecoderMock = new Mock<IContainerDecoder>();
            var codec = new ReferenceTableCodec();
            var encodedTables = new[] { EncodeTable(codec, 1), EncodeTable(codec, 2) };
            var decodeCount = 0;

            fileStoreMock.SetupGet(store => store.IndexFileCount).Returns(255);
            fileStoreMock.Setup(store => store.Read(255, 16)).Returns(() => new MemoryStream([1]));
            containerDecoderMock.Setup(decoder => decoder.Decode(It.IsAny<MemoryStream>()))
                .Returns(() => new Container(new MemoryStream(encodedTables[decodeCount++])));

            var provider = new ReferenceTableProvider(fileStoreMock.Object, containerDecoderMock.Object, codec);

            var first = provider.ReadReferenceTable(16);
            var cached = provider.ReadReferenceTable(16);

            provider.Invalidate(16);

            var refreshed = provider.ReadReferenceTable(16);

            Assert.Same(first, cached);
            Assert.NotSame(first, refreshed);
            Assert.Equal(1, first.Version);
            Assert.Equal(2, refreshed.Version);
            Assert.Equal(2, decodeCount);
        }

        private static byte[] EncodeTable(ReferenceTableCodec codec, int version)
        {
            var table = new ReferenceTable(7, version, (ReferenceTableFlags)0, 0);
            using var encoded = codec.Encode(table);
            return encoded.ToArray();
        }
    }
}
