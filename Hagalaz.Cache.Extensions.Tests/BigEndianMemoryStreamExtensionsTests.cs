using System.IO;
using Xunit;

namespace Hagalaz.Cache.Extensions.Tests
{
    public class BigEndianMemoryStreamExtensionsTests
    {
        [Fact]
        public void Remaining_ReturnsCorrectValue()
        {
            var stream = new MemoryStream(new byte[10]);
            stream.Position = 3;
            Assert.Equal(7, stream.Remaining());
        }

        [Fact]
        public void Flip_ResetsPositionAndSetsLength()
        {
            var stream = new MemoryStream();
            stream.Write(new byte[5], 0, 5);
            Assert.Equal(5, stream.Position);
            stream.Flip();
            Assert.Equal(0, stream.Position);
            Assert.Equal(5, stream.Length);
        }

        [Fact]
        public void Rewind_ResetsPosition()
        {
            var stream = new MemoryStream(new byte[10]);
            stream.Position = 5;
            stream.Rewind();
            Assert.Equal(0, stream.Position);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(short.MaxValue)]
        [InlineData(short.MaxValue - 1)]
        [InlineData(40000)]
        public void WriteAndReadBigSmart(int valueToWrite)
        {
            using var stream = new MemoryStream();
            stream.WriteBigSmart(valueToWrite);
            stream.Position = 0;
            var result = stream.ReadBigSmart();
            Assert.Equal(valueToWrite, result);
        }

        [Fact]
        public void WriteAndReadBigSmart_NegativeValue()
        {
            using var stream = new MemoryStream();
            stream.WriteBigSmart(-1);
            stream.Position = 0;
            var result = stream.ReadBigSmart();
            Assert.Equal(short.MaxValue, result);
        }

        [Fact]
        public void WriteAndReadInt()
        {
            using var stream = new MemoryStream();
            stream.WriteInt(0x12345678);
            stream.Position = 0;
            Assert.Equal(0x12345678, stream.ReadInt());
        }

        [Fact]
        public void WriteAndReadMedInt()
        {
            using var stream = new MemoryStream();
            stream.WriteMedInt(0x123456);
            stream.Position = 0;
            Assert.Equal(0x123456, stream.ReadMedInt());
        }

        [Fact]
        public void WriteAndReadShort()
        {
            using var stream = new MemoryStream();
            stream.WriteShort(0x1234);
            stream.Position = 0;
            Assert.Equal(0x1234, stream.ReadShort());
        }

        [Fact]
        public void WriteAndReadByte()
        {
            using var stream = new MemoryStream();
            stream.WriteByte(0x12);
            stream.Position = 0;
            Assert.Equal(0x12, stream.ReadByte());
        }

        [Fact]
        public void WriteAndReadSignedByte()
        {
            using var stream = new MemoryStream();
            stream.WriteSignedByte(-10);
            stream.Position = 0;
            Assert.Equal(-10, stream.ReadSignedByte());
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(127)]
        [InlineData(128)]
        public void WriteAndReadSmart(int value)
        {
            using var stream = new MemoryStream();
            stream.WriteSmart(value);
            stream.Position = 0;
            Assert.Equal(value, stream.ReadSmart());
        }

        [Fact]
        public void WriteAndReadBytes()
        {
            using var stream = new MemoryStream();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            stream.WriteBytes(data);
            stream.Position = 0;
            var result = new byte[5];
            stream.Read(result, 0, 5);
            Assert.Equal(data, result);
        }

        [Fact]
        public void WriteAndReadString()
        {
            using var stream = new MemoryStream();
            var testString = "Hello, World!";
            stream.WriteString(testString);
            stream.Position = 0;
            Assert.Equal(testString, stream.ReadString());
        }

        [Fact]
        public void ReadUnsignedByte_ReturnsCorrectValue()
        {
            var stream = new MemoryStream(new byte[] { 0xFF });
            Assert.Equal(255, stream.ReadUnsignedByte());
        }

        [Fact]
        public void ReadUnsignedShort_ReturnsCorrectValue()
        {
            var stream = new MemoryStream(new byte[] { 0x12, 0x34 });
            Assert.Equal(0x1234, stream.ReadUnsignedShort());
        }

        [Fact]
        public void ReadVString_ValidVersion_ReturnsString()
        {
            using var stream = new MemoryStream();
            var testString = "Hello";
            stream.WriteByte(0);
            stream.WriteString(testString);
            stream.Position = 0;
            Assert.Equal(testString, stream.ReadVString());
        }

        [Fact]
        public void ReadVString_InvalidVersion_ReturnsEmpty()
        {
            using var stream = new MemoryStream();
            stream.WriteByte(1);
            stream.WriteString("Hello");
            stream.Position = 0;
            Assert.Equal(string.Empty, stream.ReadVString());
        }

        [Fact]
        public void ReadCheckedString_NonEmpty_ReturnsString()
        {
            using var stream = new MemoryStream();
            var testString = "Hello";
            stream.WriteString(testString);
            stream.Position = 0;
            Assert.Equal(testString, stream.ReadCheckedString());
        }

        [Fact]
        public void ReadCheckedString_Empty_ReturnsEmpty()
        {
            using var stream = new MemoryStream();
            stream.WriteByte(0);
            stream.Position = 0;
            Assert.Equal(string.Empty, stream.ReadCheckedString());
        }

        [Fact]
        public void ReadHugeSmart_SingleValue()
        {
            using var stream = new MemoryStream();
            stream.WriteSmart(100);
            stream.Position = 0;
            Assert.Equal(100, stream.ReadHugeSmart());
        }

        [Fact]
        public void ReadHugeSmart_MultipleValues()
        {
            using var stream = new MemoryStream();
            stream.WriteSmart(32767);
            stream.WriteSmart(32767);
            stream.WriteSmart(100);
            stream.Position = 0;
            Assert.Equal(32767 + 32767 + 100, stream.ReadHugeSmart());
        }
    }
}