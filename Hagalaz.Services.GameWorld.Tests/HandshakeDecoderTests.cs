using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Security;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Decoders;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public sealed class HandshakeDecoderTests
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

        [TestMethod]
        public void TryParsePacketHeader_TrailingBytesAfterDeclaredPayload_ReturnsFalse()
        {
            var input = new byte[]
            {
                0, 8,
                0, 0, 0, 1,
                0, 0, 0, 2,
                0x7F
            };
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(input));

            var result = HandshakeDecoderHelper.TryParsePacketHeader(ref reader, out _, out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryParseXteaBlock_MultiSegmentInput_ParsesExactDecryptedPayload()
        {
            var keys = new uint[] { 1, 2, 3, 4 };
            var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var encrypted = new byte[plaintext.Length];
            XTEA.Encrypt(plaintext, encrypted, keys);
            var input = CreateSequence(encrypted[..3], encrypted[3..]);
            var reader = new SequenceReader<byte>(input);
            byte[]? parsedPayload = null;

            var result = HandshakeDecoderHelper.TryParseXteaBlock(
                ref reader,
                keys,
                (in ReadOnlySequence<byte> payload) =>
                {
                    Assert.IsTrue(payload.IsSingleSegment);
                    Assert.AreEqual(plaintext.Length, payload.Length);
                    parsedPayload = payload.ToArray();
                    return true;
                });

            Assert.IsTrue(result);
            CollectionAssert.AreEqual(plaintext, parsedPayload);
        }

        [TestMethod]
        public void TryParseXteaBlock_NonBlockAlignedPayload_ReturnsFalseWithoutInvokingParser()
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[7]));
            var parserInvoked = false;

            var result = HandshakeDecoderHelper.TryParseXteaBlock(
                ref reader,
                [1, 2, 3, 4],
                (in ReadOnlySequence<byte> _) =>
                {
                    parserInvoked = true;
                    return true;
                });

            Assert.IsFalse(result);
            Assert.IsFalse(parserInvoked);
        }

        [TestMethod]
        public void TryParseXteaBlock_ClearsUsedBufferBeforeReturningItToPool()
        {
            var keys = new uint[] { 1, 2, 3, 4 };
            var plaintext = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 };
            var encrypted = new byte[plaintext.Length];
            XTEA.Encrypt(plaintext, encrypted, keys);
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(encrypted));
            var pool = new RecordingByteArrayPool();

            var result = HandshakeDecoderHelper.TryParseXteaBlock(
                ref reader,
                keys,
                (in ReadOnlySequence<byte> _) => true,
                pool);

            Assert.IsTrue(result);
            Assert.IsTrue(pool.ReturnedWithClearArray);
            Assert.IsNotNull(pool.ReturnedBuffer);
            Assert.IsTrue(pool.ReturnedBuffer!.AsSpan(0, plaintext.Length).IndexOfAnyExcept((byte)0) < 0);
        }

        [TestMethod]
        public void TryParseHardwareBlock_TruncatedMediumValue_ReturnsFalse()
        {
            var input = new byte[14];
            input[0] = 6;
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(input));

            var result = HandshakeDecoderHelper.TryParseHardwareBlock(ref reader);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void WorldHandshakeDecoder_NonBlockAlignedEncryptedPayload_ReturnsFalse()
        {
            var rsaBlock = CreateRsaBlock();
            var decoder = new WorldHandshakeRequestDecoder(
                Options.Create(CreateRsaConfig(rsaBlock)),
                Substitute.For<ICacheAPI>());

            var result = decoder.TryDecodeMessage(CreateHandshakeInput(rsaBlock, includeWorldLoginFlag: true, encryptedPayload: new byte[7]), out var message);

            Assert.IsFalse(result);
            Assert.IsNull(message);
        }

        [TestMethod]
        public void LobbyHandshakeDecoder_NonBlockAlignedEncryptedPayload_ReturnsFalse()
        {
            var rsaBlock = CreateRsaBlock();
            var decoder = new LobbyHandshakeRequestDecoder(
                Options.Create(CreateRsaConfig(rsaBlock)),
                Substitute.For<ICacheAPI>());

            var result = decoder.TryDecodeMessage(CreateHandshakeInput(rsaBlock, includeWorldLoginFlag: false, encryptedPayload: new byte[7]), out var message);

            Assert.IsFalse(result);
            Assert.IsNull(message);
        }

        [TestMethod]
        public void WorldHandshakeDecoder_SettingsLengthBeyondPayload_ReturnsFalse()
        {
            var rsaBlock = CreateRsaBlock();
            var decoder = new WorldHandshakeRequestDecoder(
                Options.Create(CreateRsaConfig(rsaBlock)),
                Substitute.For<ICacheAPI>());
            var encryptedPayload = EncryptPayload(CreatePayloadWithTruncatedSettings(world: true));

            var result = decoder.TryDecodeMessage(
                CreateHandshakeInput(rsaBlock, includeWorldLoginFlag: true, encryptedPayload: encryptedPayload),
                out var message);

            Assert.IsFalse(result);
            Assert.IsNull(message);
        }

        [TestMethod]
        public void LobbyHandshakeDecoder_SettingsLengthBeyondPayload_ReturnsFalse()
        {
            var rsaBlock = CreateRsaBlock();
            var decoder = new LobbyHandshakeRequestDecoder(
                Options.Create(CreateRsaConfig(rsaBlock)),
                Substitute.For<ICacheAPI>());
            var encryptedPayload = EncryptPayload(CreatePayloadWithTruncatedSettings(world: false));

            var result = decoder.TryDecodeMessage(
                CreateHandshakeInput(rsaBlock, includeWorldLoginFlag: false, encryptedPayload: encryptedPayload),
                out var message);

            Assert.IsFalse(result);
            Assert.IsNull(message);
        }

        private static ReadOnlySequence<byte> CreateSequence(params byte[][] segments)
        {
            var first = new BufferSegment(segments[0]);
            var last = first;
            for (var i = 1; i < segments.Length; i++)
            {
                last = last.Append(segments[i]);
            }
            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        private static byte[] CreateRsaBlock()
        {
            var block = new List<byte> { 10 };
            AppendInt32(block, 1);
            AppendInt32(block, 2);
            AppendInt32(block, 3);
            AppendInt32(block, 4);
            AppendInt64(block, 0);
            block.AddRange(Encoding.ASCII.GetBytes("password"));
            block.Add(0);
            AppendInt64(block, 0);
            AppendInt64(block, 0);
            return block.ToArray();
        }

        private static RsaClientConfig CreateRsaConfig(byte[] rsaBlock) => new()
        {
            PrivateKey = BigInteger.One,
            ModulusKey = BigInteger.One << (rsaBlock.Length * 8)
        };

        private static byte[] CreatePayloadWithTruncatedSettings(bool world)
        {
            var payload = new List<byte> { 0 };
            AppendInt64(payload, 1);
            if (world)
            {
                payload.Add(0);
                AppendInt16(payload, 800);
                AppendInt16(payload, 600);
                payload.Add(0);
                AppendInt32(payload, 0);
            }
            else
            {
                payload.Add(0);
                payload.Add(0);
            }
            payload.AddRange(new byte[24]);
            payload.Add(0);
            payload.Add(byte.MaxValue);
            while (payload.Count % 8 != 0)
            {
                payload.Add(0);
            }
            return payload.ToArray();
        }

        private static byte[] EncryptPayload(byte[] plaintext)
        {
            var encrypted = new byte[plaintext.Length];
            XTEA.Encrypt(plaintext, encrypted, [1, 2, 3, 4]);
            return encrypted;
        }

        private static ReadOnlySequence<byte> CreateHandshakeInput(byte[] rsaBlock, bool includeWorldLoginFlag, byte[] encryptedPayload)
        {
            var input = new List<byte> { 0, 0 };
            AppendInt32(input, 742);
            AppendInt32(input, 0);
            if (includeWorldLoginFlag)
            {
                input.Add(0);
            }
            AppendInt16(input, rsaBlock.Length);
            input.AddRange(rsaBlock);
            input.AddRange(encryptedPayload);
            var packetSize = input.Count - 2;
            input[0] = (byte)(packetSize >> 8);
            input[1] = (byte)packetSize;
            return new ReadOnlySequence<byte>(input.ToArray());
        }

        private static void AppendInt16(List<byte> buffer, int value)
        {
            Span<byte> bytes = stackalloc byte[2];
            BinaryPrimitives.WriteInt16BigEndian(bytes, (short)value);
            buffer.AddRange(bytes.ToArray());
        }

        private static void AppendInt32(List<byte> buffer, int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            buffer.AddRange(bytes.ToArray());
        }

        private static void AppendInt64(List<byte> buffer, long value)
        {
            Span<byte> bytes = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(bytes, value);
            buffer.AddRange(bytes.ToArray());
        }

        private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
        {
            public BufferSegment(byte[] bytes) => Memory = bytes;

            public BufferSegment Append(byte[] bytes)
            {
                var next = new BufferSegment(bytes)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };
                Next = next;
                return next;
            }
        }

        private sealed class RecordingByteArrayPool : ArrayPool<byte>
        {
            public byte[]? ReturnedBuffer { get; private set; }
            public bool ReturnedWithClearArray { get; private set; }

            public override byte[] Rent(int minimumLength)
            {
                var buffer = new byte[Math.Max(minimumLength, 32)];
                Array.Fill(buffer, (byte)0xA5);
                return buffer;
            }

            public override void Return(byte[] array, bool clearArray = false)
            {
                ReturnedBuffer = array;
                ReturnedWithClearArray = clearArray;
            }
        }
    }
}
