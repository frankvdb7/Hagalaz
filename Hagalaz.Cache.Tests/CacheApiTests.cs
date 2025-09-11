using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class CacheApiTests
    {
        private readonly Mock<IFileStore> _fileStoreMock;
        private readonly Mock<IReferenceTableProvider> _referenceTableProviderMock;
        private readonly Mock<ICacheWriter> _cacheWriterMock;
        private readonly Mock<IContainerFactory> _containerFactoryMock;
        private readonly Mock<IReferenceTableFactory> _referenceTableFactoryMock;
        private readonly CacheApi _cacheApi;

        public CacheApiTests()
        {
            _fileStoreMock = new Mock<IFileStore>();
            _referenceTableProviderMock = new Mock<IReferenceTableProvider>();
            _cacheWriterMock = new Mock<ICacheWriter>();
            _containerFactoryMock = new Mock<IContainerFactory>();
            _referenceTableFactoryMock = new Mock<IReferenceTableFactory>();
            _cacheApi = new CacheApi(_fileStoreMock.Object, _referenceTableProviderMock.Object, _cacheWriterMock.Object, _containerFactoryMock.Object, _referenceTableFactoryMock.Object);
        }

        [Fact]
        public void GetFileId_ShouldReturnFileId()
        {
            // Arrange
            var referenceTableMock = new Mock<IReferenceTable>();
            referenceTableMock.Setup(rt => rt.GetFileId("test_file")).Returns(123);
            _referenceTableProviderMock.Setup(rtm => rtm.ReadReferenceTable(1)).Returns(referenceTableMock.Object);

            // Act
            var fileId = _cacheApi.GetFileId(1, "test_file");

            // Assert
            Assert.Equal(123, fileId);
        }

        [Fact]
        public void ReadContainer_ReturnsContainer()
        {
            // Arrange
            var expectedContainer = new Mock<IContainer>().Object;
            _containerFactoryMock.Setup(f => f.Decode(It.IsAny<System.IO.MemoryStream>())).Returns(expectedContainer);
            _fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(1);
            _fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>())).Returns(new System.IO.MemoryStream());

            // Act
            var result = _cacheApi.ReadContainer(0, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedContainer, result);
        }
    }
}
