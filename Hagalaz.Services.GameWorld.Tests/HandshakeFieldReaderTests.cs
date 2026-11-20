using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.GameWorld.Network.Handshake.Decoders;
using Hagalaz.Utilities;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    /// <summary>
    ///     Pins the field-level parsing primitives shared by the lobby and world sign-in decoders.
    /// </summary>
    [TestClass]
    public sealed class HandshakeFieldReaderTests
    {
        // ---------- TryReadLogin ----------

        [TestMethod]
        public void TryReadLogin_StringForm_ReturnsTheString()
        {
            var data = new List<byte> { 1 };
            data.AddRange(Encoding.UTF8.GetBytes("zezima"));
            data.Add(0);
            var reader = Reader(data);

            Assert.IsTrue(HandshakeDecoderHelper.TryReadLogin(ref reader, out var login));

            Assert.AreEqual("zezima", login);
            Assert.AreEqual(0, reader.Remaining);
        }

        [TestMethod]
        public void TryReadLogin_EncodedForm_DecodesTheLong()
        {
            var data = new List<byte> { 0 };
            data.AddRange(BigEndian("zezima".StringToLong()));
            var reader = Reader(data);

            Assert.IsTrue(HandshakeDecoderHelper.TryReadLogin(ref reader, out var login));

            Assert.AreEqual("zezima", login);
            Assert.AreEqual(0, reader.Remaining);
        }

        [TestMethod]
        public void TryReadLogin_TruncatedEncodedLong_ReturnsFalse()
        {
            var reader = Reader([0, 0, 0, 0]);

            Assert.IsFalse(HandshakeDecoderHelper.TryReadLogin(ref reader, out _));
        }

        [TestMethod]
        public void TryReadLogin_Empty_ReturnsFalse()
        {
            var reader = Reader([]);

            Assert.IsFalse(HandshakeDecoderHelper.TryReadLogin(ref reader, out _));
        }

        // ---------- TryReadUserId ----------

        [TestMethod]
        public void TryReadUserId_ReadsExactly24BytesAndAdvances()
        {
            var data = Enumerable.Range(1, 30).Select(i => (byte)i).ToList();
            var reader = Reader(data);

            Assert.IsTrue(HandshakeDecoderHelper.TryReadUserId(ref reader, out var userId));

            CollectionAssert.AreEqual(data.Take(24).ToArray(), userId);
            Assert.AreEqual(6, reader.Remaining);
        }

        [TestMethod]
        public void TryReadUserId_Fewer24Bytes_ReturnsFalseWithoutAdvancing()
        {
            var reader = Reader(new byte[23].ToList());

            Assert.IsFalse(HandshakeDecoderHelper.TryReadUserId(ref reader, out _));

            Assert.AreEqual(23, reader.Remaining);
        }

        // ---------- TrySkipSettingsData ----------

        [TestMethod]
        public void TrySkipSettingsData_SkipsDeclaredLength()
        {
            var reader = Reader([3, 9, 9, 9, 42]);

            Assert.IsTrue(HandshakeDecoderHelper.TrySkipSettingsData(ref reader));

            Assert.AreEqual(1, reader.Remaining);
        }

        [TestMethod]
        public void TrySkipSettingsData_ZeroLength_SkipsNothing()
        {
            var reader = Reader([0, 42]);

            Assert.IsTrue(HandshakeDecoderHelper.TrySkipSettingsData(ref reader));

            Assert.AreEqual(1, reader.Remaining);
        }

        [TestMethod]
        public void TrySkipSettingsData_LengthBeyondPayload_ReturnsFalse()
        {
            var reader = Reader([5, 1, 2]);

            Assert.IsFalse(HandshakeDecoderHelper.TrySkipSettingsData(ref reader));
        }

        [TestMethod]
        public void TrySkipSettingsData_Empty_ReturnsFalse()
        {
            var reader = Reader([]);

            Assert.IsFalse(HandshakeDecoderHelper.TrySkipSettingsData(ref reader));
        }

        // ---------- TryReadCacheCrcs ----------

        [TestMethod]
        public void TryReadCacheCrcs_ReadsOneBigEndianCrcPerIndexExcludingMaster()
        {
            var cache = CacheWithFileCount(3);
            var data = new List<byte>();
            data.AddRange(BigEndian(0x01020304));
            data.AddRange(BigEndian(-5));
            var reader = Reader(data);

            Assert.IsTrue(HandshakeDecoderHelper.TryReadCacheCrcs(ref reader, cache, out var crcs));

            CollectionAssert.AreEqual(new[] { 0x01020304, -5 }, crcs);
            Assert.AreEqual(0, reader.Remaining);
        }

        [TestMethod]
        public void TryReadCacheCrcs_MissingCrc_ReturnsFalse()
        {
            var cache = CacheWithFileCount(3);
            var reader = Reader(BigEndian(7).ToList());

            Assert.IsFalse(HandshakeDecoderHelper.TryReadCacheCrcs(ref reader, cache, out _));
        }

        [TestMethod]
        public void TryReadCacheCrcs_EmptyCache_ReturnsFalse()
        {
            var cache = CacheWithFileCount(0);
            var reader = Reader([]);

            Assert.IsFalse(HandshakeDecoderHelper.TryReadCacheCrcs(ref reader, cache, out _));
        }

        [TestMethod]
        public void TryReadCacheCrcs_SingleFileCache_ReadsNoCrcsAndSucceeds()
        {
            var cache = CacheWithFileCount(1);
            var reader = Reader([]);

            Assert.IsTrue(HandshakeDecoderHelper.TryReadCacheCrcs(ref reader, cache, out var crcs));

            Assert.IsEmpty(crcs);
        }

        private static ICacheAPI CacheWithFileCount(int count)
        {
            var cache = Substitute.For<ICacheAPI>();
            cache.GetFileCount(byte.MaxValue).Returns(count);
            return cache;
        }

        private static SequenceReader<byte> Reader(IEnumerable<byte> bytes) => new(new ReadOnlySequence<byte>(bytes.ToArray()));

        private static byte[] BigEndian(long value)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(bytes, value);
            return bytes;
        }

        private static byte[] BigEndian(int value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            return bytes;
        }
    }
}
