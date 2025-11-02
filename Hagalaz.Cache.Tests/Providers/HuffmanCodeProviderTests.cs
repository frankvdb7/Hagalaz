using Hagalaz.Cache.Abstractions;
using NSubstitute;
using Xunit;
using Hagalaz.Cache.Abstractions.Types.Providers.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;

namespace Hagalaz.Cache.Tests.Providers
{
    public class HuffmanCodeProviderTests
    {
        private readonly ICacheAPI _cacheApi;
        private readonly HuffmanCodeProvider _huffmanCodeProvider;

        public HuffmanCodeProviderTests()
        {
            _cacheApi = Substitute.For<ICacheAPI>();
            _huffmanCodeProvider = new HuffmanCodeProvider(_cacheApi);
        }

        [Fact]
        public void GetCoding_WithValidData_ReturnsHuffmanCoding()
        {
            // Arrange
            var data = new byte[] { 1, 2, 3 };
            var container = new Container(new MemoryStream(data));
            _cacheApi.GetFileId(10, "huffman").Returns(1);
            _cacheApi.ReadContainer(10, 1).Returns(container);

            // Act
            var result = _huffmanCodeProvider.GetCoding();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<HuffmanCoding>(result);
        }

        [Fact]
        public void GetCoding_WithEmptyData_ReturnsHuffmanCoding()
        {
            // Arrange
            var data = new byte[0];
            var container = new Container(new MemoryStream(data));
            _cacheApi.GetFileId(10, "huffman").Returns(1);
            _cacheApi.ReadContainer(10, 1).Returns(container);

            // Act
            var result = _huffmanCodeProvider.GetCoding();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<HuffmanCoding>(result);
        }

        [Fact]
        public void GetCoding_CachesResult()
        {
            // Arrange
            var data = new byte[] { 1, 2, 3 };
            var container = new Container(new MemoryStream(data));
            _cacheApi.GetFileId(10, "huffman").Returns(1);
            _cacheApi.ReadContainer(10, 1).Returns(container);

            // Act
            var result1 = _huffmanCodeProvider.GetCoding();
            var result2 = _huffmanCodeProvider.GetCoding();

            // Assert
            Assert.Same(result1, result2);
            _cacheApi.Received(1).ReadContainer(10, 1);
        }
    }
}
