using Moq;
using System.IO;
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
            var containerFactoryMock = new Mock<IContainerDecoder>();
            var referenceTableFactoryMock = new Mock<IReferenceTableDecoder>();

            var containerMock = new Mock<IContainer>();
            containerMock.Setup(c => c.Data).Returns(new MemoryStream());

            var referenceTableMock = new Mock<IReferenceTable>();

            fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(255);
            fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
            containerFactoryMock.Setup(cf => cf.Decode(It.IsAny<MemoryStream>())).Returns(containerMock.Object);
            referenceTableFactoryMock.Setup(rtf => rtf.Decode(It.IsAny<MemoryStream>(), It.IsAny<bool>())).Returns(referenceTableMock.Object);

            var provider = new ReferenceTableProvider(fileStoreMock.Object, containerFactoryMock.Object, referenceTableFactoryMock.Object);

            // Act
            var result = provider.ReadReferenceTable(1);

            // Assert
            Assert.Same(referenceTableMock.Object, result);
        }
    }
}
