using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Cache.Extensions.Tests
{
[TestClass]
    public class BigEndianMemoryStreamExtensionsTests
    {
        [TestMethod]
        public void Remaining_ReturnsCorrectValue()
        {
            var stream = new MemoryStream(new byte[10]);
            stream.Position = 3;
            Assert.AreEqual(7, stream.Remaining());
        }

        [TestMethod]
        public void Flip_ResetsPositionAndSetsLength()
        {
            var stream = new MemoryStream();
            stream.Write(new byte[5], 0, 5);
            Assert.AreEqual(5, stream.Position);
            stream.Flip();
            Assert.AreEqual(0, stream.Position);
            Assert.AreEqual(5, stream.Length);
        }

        [TestMethod]
        public void Rewind_ResetsPosition()
        {
            var stream = new MemoryStream(new byte[10]);
            stream.Position = 5;
            stream.Rewind();
            Assert.AreEqual(0, stream.Position);
        }

        [TestMethod]
        [DataRow(100)]
        [DataRow(short.MaxValue)]
        [DataRow(short.MaxValue - 1)]
        [DataRow(40000)]
        [DataRow(0)]
        [DataRow(int.MaxValue)]
        public void WriteAndReadBigSmart(int valueToWrite)
        {
            using var stream = new MemoryStream();
            stream.WriteBigSmart(valueToWrite);
            stream.Position = 0;
            var result = stream.ReadBigSmart();
            Assert.AreEqual(valueToWrite, result);
        }

        [TestMethod]
        public void WriteAndReadBigSmart_NegativeValue()
        {
            using var stream = new MemoryStream();
            stream.WriteBigSmart(-1);
            stream.Position = 0;
            var result = stream.ReadBigSmart();
            Assert.AreEqual(short.MaxValue, result);
        }

        [TestMethod]
        public void WriteAndReadInt()
        {
            using var stream = new MemoryStream();
            stream.WriteInt(0x12345678);
            stream.Position = 0;
            Assert.AreEqual(0x12345678, stream.ReadInt());
        }

        [TestMethod]
        [DataRow(0L)]
        [DataRow(1234567890123456789L)]
        [DataRow(-1234567890123456789L)]
        [DataRow(long.MaxValue)]
        [DataRow(long.MinValue)]
        public void WriteAndReadLong(long value)
        {
            using var stream = new MemoryStream();
            stream.WriteLong(value);
            stream.Position = 0;
            Assert.AreEqual(value, stream.ReadLong());
        }

        [TestMethod]
        public void WriteAndReadMedInt()
        {
            using var stream = new MemoryStream();
            stream.WriteMedInt(0x123456);
            stream.Position = 0;
            Assert.AreEqual(0x123456, stream.ReadMedInt());
        }

        [TestMethod]
        public void WriteAndReadShort()
        {
            using var stream = new MemoryStream();
            stream.WriteShort(0x1234);
            stream.Position = 0;
            Assert.AreEqual(0x1234, stream.ReadShort());
        }

        [TestMethod]
        public void WriteAndReadByte()
        {
            using var stream = new MemoryStream();
            stream.WriteByte(0x12);
            stream.Position = 0;
            Assert.AreEqual(0x12, stream.ReadByte());
        }

        [TestMethod]
        public void WriteAndReadSignedByte()
        {
            using var stream = new MemoryStream();
            stream.WriteSignedByte(-10);
            stream.Position = 0;
            Assert.AreEqual(-10, stream.ReadSignedByte());
        }

        [TestMethod]
        [DataRow(100)]
        [DataRow(200)]
        [DataRow(127)]
        [DataRow(128)]
        [DataRow(0)]
        [DataRow(32767)]
        public void WriteAndReadSmart(int value)
        {
            using var stream = new MemoryStream();
            stream.WriteSmart(value);
            stream.Position = 0;
            Assert.AreEqual(value, stream.ReadSmart());
        }

        [TestMethod]
        public void WriteAndReadBytes()
        {
            using var stream = new MemoryStream();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            stream.WriteBytes(data);
            stream.Position = 0;
            var result = new byte[5];
            stream.Read(result, 0, 5);
            CollectionAssert.AreEqual(data, result);
        }

        [TestMethod]
        public void WriteAndReadString()
        {
            using var stream = new MemoryStream();
            var testString = "Hello, World!";
            stream.WriteString(testString);
            stream.Position = 0;
            Assert.AreEqual(testString, stream.ReadString());
        }

        [TestMethod]
        public void ReadUnsignedByte_ReturnsCorrectValue()
        {
            var stream = new MemoryStream(new byte[] { 0xFF });
            Assert.AreEqual(255, stream.ReadUnsignedByte());
        }

        [TestMethod]
        public void ReadUnsignedShort_ReturnsCorrectValue()
        {
            var stream = new MemoryStream(new byte[] { 0x12, 0x34 });
            Assert.AreEqual(0x1234, stream.ReadUnsignedShort());
        }

        [TestMethod]
        public void ReadVString_ValidVersion_ReturnsString()
        {
            using var stream = new MemoryStream();
            var testString = "Hello";
            stream.WriteByte(0);
            stream.WriteString(testString);
            stream.Position = 0;
            Assert.AreEqual(testString, stream.ReadVString());
        }

        [TestMethod]
        public void ReadVString_InvalidVersion_ReturnsEmpty()
        {
            using var stream = new MemoryStream();
            stream.WriteByte(1);
            stream.WriteString("Hello");
            stream.Position = 0;
            Assert.AreEqual(string.Empty, stream.ReadVString());
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("Hello, World!")]
        [DataRow("`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?")]
        public void WriteAndReadVString(string value)
        {
            using var stream = new MemoryStream();
            stream.WriteVString(value);
            stream.Position = 0;
            Assert.AreEqual(value, stream.ReadVString());
        }

        [TestMethod]
        public void ReadCheckedString_NonEmpty_ReturnsString()
        {
            using var stream = new MemoryStream();
            var testString = "Hello";
            stream.WriteString(testString);
            stream.Position = 0;
            Assert.AreEqual(testString, stream.ReadCheckedString());
        }

        [TestMethod]
        public void ReadCheckedString_Empty_ReturnsEmpty()
        {
            using var stream = new MemoryStream();
            stream.WriteByte(0);
            stream.Position = 0;
            Assert.AreEqual(string.Empty, stream.ReadCheckedString());
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("Hello, World!")]
        [DataRow("`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?")]
        public void WriteAndReadCheckedString(string value)
        {
            using var stream = new MemoryStream();
            stream.WriteCheckedString(value);
            stream.Position = 0;
            Assert.AreEqual(value, stream.ReadCheckedString());
        }

        [TestMethod]
        public void ReadHugeSmart_SingleValue()
        {
            using var stream = new MemoryStream();
            stream.WriteSmart(100);
            stream.Position = 0;
            Assert.AreEqual(100, stream.ReadHugeSmart());
        }

        [TestMethod]
        public void ReadHugeSmart_MultipleValues()
        {
            using var stream = new MemoryStream();
            stream.WriteSmart(32767);
            stream.WriteSmart(32767);
            stream.WriteSmart(100);
            stream.Position = 0;
            Assert.AreEqual(32767 + 32767 + 100, stream.ReadHugeSmart());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(100)]
        [DataRow(32766)]
        [DataRow(32767)]
        [DataRow(65534)]
        public void WriteAndReadHugeSmart(int value)
        {
            using var stream = new MemoryStream();
            stream.WriteHugeSmart(value);
            stream.Position = 0;
            Assert.AreEqual(value, stream.ReadHugeSmart());
        }
    }
}