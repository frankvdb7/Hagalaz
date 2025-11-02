using Moq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class CacheWriterTests
    {
        private readonly Mock<IFileStore> _fileStoreMock;
        private readonly Mock<IReferenceTableProvider> _referenceTableProviderMock;
        private readonly Mock<IContainerDecoder> _containerDecoderMock;
        private readonly Mock<IReferenceTableCodec> _referenceTableCodecMock;
        private readonly CacheWriter _cacheWriter;

        public CacheWriterTests()
        {
            _fileStoreMock = new Mock<IFileStore>();
            _referenceTableProviderMock = new Mock<IReferenceTableProvider>();
            _containerDecoderMock = new Mock<IContainerDecoder>();
            _referenceTableCodecMock = new Mock<IReferenceTableCodec>();
            _cacheWriter = new CacheWriter(_fileStoreMock.Object, _referenceTableProviderMock.Object, _containerDecoderMock.Object, _referenceTableCodecMock.Object);
        }

        [Fact]
        public void Write_ShouldWriteToStore()
        {
            // Arrange
            var containerMock = new Mock<IContainer>();
            containerMock.Setup(c => c.Encode(false)).Returns(new byte[0]);
            var referenceTableMock = new Mock<IReferenceTable>();
            _referenceTableCodecMock.Setup(rt => rt.Encode(referenceTableMock.Object)).Returns(new MemoryStream());
            _referenceTableProviderMock.Setup(rtm => rtm.ReadReferenceTable(1)).Returns(referenceTableMock.Object);

            var oldContainerMock = new Mock<IContainer>();
            oldContainerMock.Setup(c => c.CompressionType).Returns(CompressionType.None);
            _fileStoreMock.Setup(fs => fs.Read(255, 1)).Returns(new MemoryStream());
            _containerDecoderMock.Setup(cf => cf.Decode(It.IsAny<MemoryStream>())).Returns(oldContainerMock.Object);

            // Act
            _cacheWriter.Write(1, 1, containerMock.Object);

            // Assert
            _fileStoreMock.Verify(fs => fs.Write(1, 1, It.IsAny<MemoryStream>()), Times.Once);
        }
    }
}
