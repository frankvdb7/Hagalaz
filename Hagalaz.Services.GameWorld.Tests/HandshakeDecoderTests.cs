using System.Buffers;
using System.Numerics;
using Hagalaz.Services.GameWorld.Network.Handshake.Decoders;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class HandshakeDecoderTests
    {
        [TestMethod]
        public void TryParseRsaHeader_ValidData_ReturnsTrue()
        {
            // Arrange
            var privateKey = new BigInteger(7); // Example private key
            var modulusKey = new BigInteger(33); // Example modulus key

            var rsaData = new byte[] { 0x00, 0x03, 0x02, 0x03, 0x04 }; // Header size = 3, data = { 0x02, 0x03, 0x04 }
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(rsaData));

            // Compute the expected value manually
            var candidateBigInteger = new BigInteger([0x02, 0x03, 0x04], isUnsigned: false, isBigEndian: true);
            var expectedValue = BigInteger.ModPow(candidateBigInteger, privateKey, modulusKey);

            // Act
            var result = HandshakeDecoderHelper.TryParseRsaHeader(ref reader, privateKey, modulusKey, out BigInteger rsaBigInteger);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(expectedValue, rsaBigInteger);
            Assert.AreEqual(0, reader.Remaining); // Validate that the reader advanced correctly
        }

        [TestMethod]
        public void TryParseRsaHeader_InvalidHeaderSize_ReturnsFalse()
        {
            // Arrange
            var privateKey = new BigInteger(7);
            var modulusKey = new BigInteger(33);
            var rsaData = new byte[] { 0x00, 0x05, 0x02 }; // Header size = 5, but not enough data
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(rsaData));

            // Act
            var result = HandshakeDecoderHelper.TryParseRsaHeader(ref reader, privateKey, modulusKey, out BigInteger rsaBigInteger);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(BigInteger.Zero, rsaBigInteger);
        }

        [TestMethod]
        public void TryParseRsaHeader_InvalidDataFormat_ReturnsFalse()
        {
            // Arrange
            var privateKey = new BigInteger(7);
            var modulusKey = new BigInteger(33);
            var rsaData = new byte[] { 0x00, 0x03, 0xFF, 0xFF, 0xFF }; // Invalid encrypted data
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(rsaData));

            // Act
            var result = HandshakeDecoderHelper.TryParseRsaHeader(ref reader, privateKey, modulusKey, out BigInteger rsaBigInteger);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(BigInteger.MinusOne, rsaBigInteger);
        }
    }
}