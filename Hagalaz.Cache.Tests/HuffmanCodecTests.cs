using System.Buffers;
using System.Reflection;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Abstractions.Types.Providers.Model;
using Hagalaz.Cache.Logic.Codecs;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class HuffmanCodecTests
    {
        private readonly Mock<IHuffmanCodeProvider> _huffmanCodeProviderMock;
        private readonly HuffmanCodec _huffmanCodec;
        private readonly HuffmanCoding _realHuffmanCoding;

        public HuffmanCodecTests()
        {
            _huffmanCodeProviderMock = new Mock<IHuffmanCodeProvider>();

            var securityAssembly = Assembly.Load("Hagalaz.Security");
            var huffmanType = securityAssembly.GetType("Hagalaz.Security.Huffman");

            Assert.NotNull(huffmanType);

            var bitSizesField = huffmanType.GetField("_huffmanBitSizes", BindingFlags.NonPublic | BindingFlags.Static);
            var masksField = huffmanType.GetField("_huffmanMasks", BindingFlags.NonPublic | BindingFlags.Static);
            var decryptKeysField = huffmanType.GetField("_huffmanDecryptKeys", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(bitSizesField);
            Assert.NotNull(masksField);
            Assert.NotNull(decryptKeysField);

            var bitSizes = (byte[])bitSizesField.GetValue(null)!;
            var masks = (int[])masksField.GetValue(null)!;
            var decryptKeys = (int[])decryptKeysField.GetValue(null)!;

            _realHuffmanCoding = new HuffmanCoding(bitSizes, masks, decryptKeys);
            _huffmanCodeProviderMock.Setup(p => p.GetCoding()).Returns(_realHuffmanCoding);

            _huffmanCodec = new HuffmanCodec(_huffmanCodeProviderMock.Object);
        }

        [Theory]
        [InlineData("this is a test")]
        [InlineData("another test with more characters!@#$%^&*()")]
        [InlineData("Lorem ipsum dolor sit amet, consectetur adipiscing elit.")]
        public void Encode_And_Decode_SimpleString_ShouldMatch(string testString)
        {
            // Arrange
            var bufferWriter = new ArrayBufferWriter<byte>();
            _huffmanCodeProviderMock.Setup(p => p.GetCoding()).Returns(_realHuffmanCoding);

            // Act
            _huffmanCodec.Encode(testString, bufferWriter);
            var encodedData = new ReadOnlySequence<byte>(bufferWriter.WrittenMemory);
            var success = _huffmanCodec.TryDecode(in encodedData, testString.Length, out var decodedString);

            // Assert
            Assert.True(success);
            Assert.Equal(testString, decodedString);
        }

        [Fact]
        public void TryDecode_EmptyInput_ShouldReturnEmptyString()
        {
            // Arrange
            var emptySequence = new ReadOnlySequence<byte>();

            // Act
            var success = _huffmanCodec.TryDecode(in emptySequence, 0, out var decodedString);

            // Assert
            Assert.True(success);
            Assert.Equal(string.Empty, decodedString);
        }

        [Fact]
        public void Encode_StringWithUncodableCharacter_ThrowsInvalidOperationException()
        {
            // Arrange
            var bitSizes = (byte[])_realHuffmanCoding.BitSizes.Clone();
            bitSizes['X'] = 0; // Make 'X' an uncodable character for this test
            var customHuffmanCoding = new HuffmanCoding(bitSizes, _realHuffmanCoding.Masks, _realHuffmanCoding.DecryptKeys);
            _huffmanCodeProviderMock.Setup(p => p.GetCoding()).Returns(customHuffmanCoding);

            var uncodableString = "a string with an uncodable character X";
            var bufferWriter = new ArrayBufferWriter<byte>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _huffmanCodec.Encode(uncodableString, bufferWriter));

            // Reset mock for other tests that might run in parallel
            _huffmanCodeProviderMock.Setup(p => p.GetCoding()).Returns(_realHuffmanCoding);
        }

    }
}