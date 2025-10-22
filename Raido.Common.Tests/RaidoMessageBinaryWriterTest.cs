using Raido.Common.Buffers;
using Raido.Common.Protocol;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Raido.Common.Tests
{
    [TestClass]
    public class RaidoMessageBinaryWriterTest
    {
        private class MockByteBufferWriter : IByteBufferWriter
        {
            public bool AdvancedCalled { get; private set; }
            public bool GetMemoryCalled { get; private set; }
            public bool GetSpanCalled { get; private set; }
            public bool WriteCalled { get; private set; }
            public bool WriteByteCalled { get; private set; }

            public long Length => 0;

            public void Advance(int count) => AdvancedCalled = true;
            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                GetMemoryCalled = true;
                return new byte[sizeHint > 0 ? sizeHint : 1];
            }
            public Span<byte> GetSpan(int sizeHint = 0)
            {
                GetSpanCalled = true;
                return new byte[sizeHint > 0 ? sizeHint : 1];
            }
            public IByteBufferWriter Write(ReadOnlySpan<byte> span)
            {
                WriteCalled = true;
                return this;
            }
            public IByteBufferWriter WriteByte(byte value)
            {
                WriteByteCalled = true;
                return this;
            }
        }

        [TestMethod]
        public void TestPassThroughMethods()
        {
            // Arrange
            var mockBuffer = new MockByteBufferWriter();
            var writer = new RaidoMessageBinaryWriter(mockBuffer);
            var data = new byte[] { 1, 2, 3 };

            // Act
            writer.Advance(1);
            writer.GetMemory(1);
            writer.GetSpan(1);
            writer.Write(data);
            writer.WriteByte(4);

            // Assert
            Assert.IsTrue(mockBuffer.AdvancedCalled);
            Assert.IsTrue(mockBuffer.GetMemoryCalled);
            Assert.IsTrue(mockBuffer.GetSpanCalled);
            Assert.IsTrue(mockBuffer.WriteCalled);
            Assert.IsTrue(mockBuffer.WriteByteCalled);
        }

        [TestMethod]
        public void SetOpcode()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.SetOpcode(123);
            Assert.AreEqual(123, output.Opcode);
        }

        [TestMethod]
        public void SetSize()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.SetSize(RaidoMessageSize.VariableByte);
            Assert.AreEqual(RaidoMessageSize.VariableByte, output.Size);
        }

        [TestMethod]
        public void BeginBitAccess()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            buffer.WriteByte(5);
            buffer.WriteByte(6);
            output.BeginBitAccess();

            Assert.AreEqual(output.Length, 2);
        }

        [TestMethod]
        public void BeginBitAccess_NoData()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();

            Assert.AreEqual(output.Length, 0);
        }

        [TestMethod]
        public void WriteBits_InvalidBitCount_ThrowsException()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => output.WriteBits(0, 1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => output.WriteBits(33, 1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => output.WriteBits(-1, 1));
        }

        [TestMethod]
        public void WriteBits()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.WriteBits(30, 5001243);

            Assert.AreEqual(3, output.Length);
        }

        [TestMethod]
        public void WriteBits_SingleByte()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(1, 1);
            output.WriteBits(1, 1);
            output.WriteBits(2, 0);
            output.EndBitAccess();

            Assert.AreEqual(1, output.Length);

            output.BeginBitAccess();
            output.WriteBits(1, 1);
            output.WriteBits(2, 0);
            output.EndBitAccess();

            Assert.AreEqual(2, output.Length);

            var expected = new byte[2];
            expected[0] = 192;
            expected[1] = 128;
            var actual = buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [Ignore("This test is fragile and non-deterministic. It needs to be rewritten.")]
        public void WriteBits_LargeData()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(30, 5001243);
            // simulates enter world packet
            for (var i = 1; i < 2048; i++)
            {
                if (i == 1)
                {
                    continue;
                }
                output.WriteBits(18, 0);
            }

            output.EndBitAccess();

            var targetBitPosition = 30 + 2046 * 18;
            var targetBytePosition = (targetBitPosition + 7) / 8;
            Assert.AreEqual(targetBytePosition, output.Length);

            var expected = new byte[targetBytePosition];
            expected[0] = 1;
            expected[1] = 49;
            expected[2] = 64;
            var actual = buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void WriteBits_Multi()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            // simulates player render skip
            output.BeginBitAccess();
            output.WriteBits(1, 0);
            output.WriteBits(2, 3);
            output.WriteBits(11, 2047);
            output.EndBitAccess();

            var targetBitPosition = 14;
            var targetBytePosition = (targetBitPosition + 7) / 8;
            Assert.AreEqual(targetBytePosition, output.Length);

            output.BeginBitAccess();
            output.WriteBits(1, 0);
            output.WriteBits(2, 3);
            output.WriteBits(11, 2047);
            output.EndBitAccess();

            targetBitPosition = 28;
            targetBytePosition = (targetBitPosition + 7) / 8;
            Assert.AreEqual(targetBytePosition, output.Length);

            var expected = new byte[4];
            expected[0] = 127;
            expected[1] = 252;
            expected[2] = 127;
            expected[3] = 252;
            var actual = buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WriteBits_CrossByte()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(4, 0b1111);
            output.WriteBits(8, 0b10101010);
            output.WriteBits(4, 0);
            output.EndBitAccess();

            Assert.AreEqual(2, output.Length);

            var expected = new byte[2];
            expected[0] = 250;
            expected[1] = 160;
            var actual = buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EndBitAccess()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(30, 5001243);
            output.EndBitAccess();

            Assert.AreEqual(4, output.Length);
        }

        [TestMethod]
        public void EndBitAccess_NoData()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.EndBitAccess();

            Assert.AreEqual(0, output.Length);
        }

        [TestMethod]
        public void EndBitAccess_Multi()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(1, 1);
            output.WriteBits(1, 1);
            output.WriteBits(2, 0);
            output.EndBitAccess();
            var expected = output.Length;

            output.BeginBitAccess();
            output.EndBitAccess();

            Assert.AreEqual(expected, output.Length);
        }

        [TestMethod]
        public void WriteBits_32Bits()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(32, -1);
            output.EndBitAccess();

            Assert.AreEqual(4, output.Length);
            var expected = new byte[] { 255, 255, 255, 255 };
            var actual = buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WriteBits_ExactByteBoundary()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(8, 0b10101010);
            output.EndBitAccess();

            Assert.AreEqual(1, output.Length);
            var expected = new byte[] { 0b10101010 };
            var actual = buffer.ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EndBitAccess_Alignment()
        {
            using var buffer = MemoryBufferWriter.Get();
            var output = new RaidoMessageBinaryWriter(buffer);
            output.BeginBitAccess();
            output.WriteBits(3, 5);
            output.EndBitAccess();

            Assert.AreEqual(1, output.Length);
        }
    }
}
