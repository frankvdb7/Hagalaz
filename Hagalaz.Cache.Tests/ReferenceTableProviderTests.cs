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
    }
}
