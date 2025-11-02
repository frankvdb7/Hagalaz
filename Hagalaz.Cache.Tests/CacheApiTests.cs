using Moq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class MemoryStreamSpy : MemoryStream
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    public class CacheApiTests
    {
        private readonly Mock<IFileStore> _fileStoreMock;
        private readonly Mock<IReferenceTableProvider> _referenceTableProviderMock;
        private readonly Mock<ICacheWriter> _cacheWriterMock;
        private readonly Mock<IContainerDecoder> _containerDecoderMock;
        private readonly Mock<IReferenceTableCodec> _referenceTableCodecMock;
        private readonly Mock<IArchiveDecoder> _archiveDecoderMock;
        private readonly CacheApi _cacheApi;

        public CacheApiTests()
        {
            _fileStoreMock = new Mock<IFileStore>();
            _referenceTableProviderMock = new Mock<IReferenceTableProvider>();
            _cacheWriterMock = new Mock<ICacheWriter>();
            _containerDecoderMock = new Mock<IContainerDecoder>();
            _referenceTableCodecMock = new Mock<IReferenceTableCodec>();
            _archiveDecoderMock = new Mock<IArchiveDecoder>();
            _cacheApi = new CacheApi(_fileStoreMock.Object, _referenceTableProviderMock.Object, _cacheWriterMock.Object, _containerDecoderMock.Object, _referenceTableCodecMock.Object, _archiveDecoderMock.Object);
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
            _containerDecoderMock.Setup(f => f.Decode(It.IsAny<System.IO.MemoryStream>())).Returns(expectedContainer);
            _fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(1);
            _fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>())).Returns(new System.IO.MemoryStream());

            // Act
            var result = _cacheApi.ReadContainer(0, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedContainer, result);
        }

        [Fact]
        public void ReadContainer_ShouldDisposeStream()
        {
            // Arrange
            var streamSpy = new MemoryStreamSpy();
            _fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(1);
            _fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>())).Returns(streamSpy);
            _containerDecoderMock.Setup(f => f.Decode(It.IsAny<System.IO.MemoryStream>())).Returns(new Mock<IContainer>().Object);

            // Act
            _cacheApi.ReadContainer(0, 0);

            // Assert
            Assert.True(streamSpy.IsDisposed);
        }
    }
}
