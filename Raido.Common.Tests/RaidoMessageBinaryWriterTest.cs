using Raido.Common.Buffers;
using Raido.Common.Protocol;

namespace Raido.Common.Tests
{
    [TestClass]
    public class RaidoMessageBinaryWriterTest
    {
        private MemoryBufferWriter _buffer = default!;
        private RaidoMessageBinaryWriter _output = default!;

        [TestInitialize]
        public void Initialize()
        {
            _buffer = MemoryBufferWriter.Get();
            _output = new RaidoMessageBinaryWriter(_buffer);
        }

        [TestCleanup]
        public void Cleanup()
        {
            MemoryBufferWriter.Return(_buffer);
        }

        [TestMethod]
        public void BeginBitAccess() 
        {
            _buffer.WriteByte(5);
            _buffer.WriteByte(6);
            _output.BeginBitAccess();

            Assert.AreEqual(_output.Length, 2);
        }

        [TestMethod]
        public void BeginBitAccess_NoData()
        {
            _output.BeginBitAccess();

            Assert.AreEqual(_output.Length, 0);
        }

        [TestMethod]
        public void WriteBits()
        {
            _output.WriteBits(30, 5001243);

            Assert.AreEqual(3, _output.Length);
        }

        [TestMethod]
        public void WriteBits_SingleByte()
        {
            _output.BeginBitAccess();
            _output.WriteBits(1, 1);
            _output.WriteBits(1, 1);
            _output.WriteBits(2, 0);
            _output.EndBitAccess();

            Assert.AreEqual(1, _output.Length);

            _output.BeginBitAccess();
            _output.WriteBits(1, 1);
            _output.WriteBits(2, 0);
            _output.EndBitAccess();

            Assert.AreEqual(2, _output.Length);

            var expected = new byte[2];
            expected[0] = 192; // this value changes for some reason if multiple tests run at once
            expected[1] = 128;
            var actual = _buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WriteBits_LargeData()
        {
            _output.BeginBitAccess();
            _output.WriteBits(30, 5001243);
            // simulates enter world packet
            for (var i = 1; i < 2048; i++)
            {
                if (i == 1)
                {
                    continue;
                }
                _output.WriteBits(18, 0);
            }

            _output.EndBitAccess();           

            var targetBitPosition = 30 + 2046 * 18;
            var targetBytePosition = (targetBitPosition + 7) / 8;
            Assert.AreEqual(targetBytePosition, _output.Length);

            var expected = new byte[targetBytePosition];
            expected[0] = 1;
            expected[1] = 49;
            expected[2] = 64;
            expected[3] = 108;
            var actual = _buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void WriteBits_Multi()
        {
            // simulates player render skip
            _output.BeginBitAccess();
            _output.WriteBits(1, 0);
            _output.WriteBits(2, 3);
            _output.WriteBits(11, 2047);
            _output.EndBitAccess();

            var targetBitPosition = 14;
            var targetBytePosition = (targetBitPosition + 7) / 8;
            Assert.AreEqual(targetBytePosition, _output.Length);

            _output.BeginBitAccess();
            _output.WriteBits(1, 0);
            _output.WriteBits(2, 3);
            _output.WriteBits(11, 2047);
            _output.EndBitAccess();

            targetBitPosition = 28;
            targetBytePosition = (targetBitPosition + 7) / 8;
            Assert.AreEqual(targetBytePosition, _output.Length);

            var expected = new byte[4];
            expected[0] = 127;
            expected[1] = 252;
            expected[2] = 127;
            expected[3] = 252;
            var actual = _buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EndBitAccess()
        {
            _output.BeginBitAccess();
            _output.WriteBits(30, 5001243);
            _output.EndBitAccess();

            Assert.AreEqual(4, _output.Length);
        }

        [TestMethod]
        public void EndBitAccess_NoData()
        {
            _output.BeginBitAccess();
            _output.EndBitAccess();

            Assert.AreEqual(0, _output.Length);
        }

        [TestMethod]
        public void EndBitAccess_Multi()
        {
            _output.BeginBitAccess();
            _output.WriteBits(1, 1);
            _output.WriteBits(1, 1);
            _output.WriteBits(2, 0);
            _output.EndBitAccess();
            var expected = _output.Length;

            _output.BeginBitAccess();
            _output.EndBitAccess();

            Assert.AreEqual(expected, _output.Length);
        }
    }
}