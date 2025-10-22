using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Buffers;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Raido.Common.Tests
{
    [TestClass]
    public class MemoryBufferWriterTests
    {
        [TestMethod]
        public void WriteByte_And_ToArray_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            writer.WriteByte(1);
            writer.WriteByte(2);
            writer.WriteByte(3);

            // Act
            var result = writer.ToArray();

            // Assert
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void Write_Span_And_ToArray_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            writer.Write(data.AsSpan());

            // Act
            var result = writer.ToArray();

            // Assert
            CollectionAssert.AreEqual(data, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void GetMemory_And_Advance_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var memory = writer.GetMemory(5);
            var data = new byte[] { 1, 2, 3, 4, 5 };
            data.CopyTo(memory);

            // Act
            writer.Advance(5);
            var result = writer.ToArray();

            // Assert
            CollectionAssert.AreEqual(data, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void Reset_ClearsBuffer()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            writer.WriteByte(1);

            // Act
            writer.Reset();
            var result = writer.ToArray();

            // Assert
            Assert.AreEqual(0, result.Length);
            MemoryBufferWriter.Return(writer);
        }


        [TestMethod]
        public async Task FlushAndDispose_DoNotThrow()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();

            // Act
            writer.Flush();
            await writer.FlushAsync(default);
            writer.Dispose();

            // Assert
            // No exception thrown
        }

        [TestMethod]
        public void GetAndReturn_CacheInstance()
        {
            // Act
            var writer1 = MemoryBufferWriter.Get();
            MemoryBufferWriter.Return(writer1);
            var writer2 = MemoryBufferWriter.Get();

            // Assert
            Assert.AreSame(writer1, writer2);
            MemoryBufferWriter.Return(writer2);
        }

        [TestMethod]
        public void Write_ByteArray_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            writer.Write(data, 0, data.Length);
            var result = writer.ToArray();

            // Assert
            CollectionAssert.AreEqual(data, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void Write_ByteArray_MultiSegment_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data1 = new byte[4096];
            var data2 = new byte[100];
            new Random().NextBytes(data1);
            new Random().NextBytes(data2);

            // Act
            writer.Write(data1, 0, data1.Length);
            writer.Write(data2, 0, data2.Length);
            var result = writer.ToArray();
            var expected = new byte[data1.Length + data2.Length];
            data1.CopyTo(expected, 0);
            data2.CopyTo(expected, data1.Length);

            // Assert
            CollectionAssert.AreEqual(expected, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void CopyTo_Span_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            writer.Write(data);
            var destination = new byte[data.Length];

            // Act
            writer.CopyTo(destination.AsSpan());

            // Assert
            CollectionAssert.AreEqual(data, destination);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void CopyTo_Span_MultiSegment_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data1 = new byte[4096];
            var data2 = new byte[100];
            new Random().NextBytes(data1);
            new Random().NextBytes(data2);
            writer.Write(data1);
            writer.Write(data2);
            var destination = new byte[data1.Length + data2.Length];
            var expected = new byte[data1.Length + data2.Length];
            data1.CopyTo(expected, 0);
            data2.CopyTo(expected, data1.Length);

            // Act
            writer.CopyTo(destination.AsSpan());

            // Assert
            CollectionAssert.AreEqual(expected, destination);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void ToArray_MultiSegment_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data1 = new byte[4096];
            var data2 = new byte[100];
            new Random().NextBytes(data1);
            new Random().NextBytes(data2);
            writer.Write(data1);
            writer.Write(data2);
            var expected = new byte[data1.Length + data2.Length];
            data1.CopyTo(expected, 0);
            data2.CopyTo(expected, data1.Length);

            // Act
            var result = writer.ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void Write_MultiSegment_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data1 = new byte[4096];
            var data2 = new byte[100];
            new Random().NextBytes(data1);
            new Random().NextBytes(data2);

            // Act
            writer.Write(data1);
            writer.Write(data2);
            var result = writer.ToArray();
            var expected = new byte[data1.Length + data2.Length];
            data1.CopyTo(expected, 0);
            data2.CopyTo(expected, data1.Length);

            // Assert
            CollectionAssert.AreEqual(expected, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void CopyTo_IBufferWriter_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            writer.Write(data);
            var destination = new ArrayBufferWriter<byte>();

            // Act
            writer.CopyTo(destination);
            var result = destination.WrittenSpan.ToArray();

            // Assert
            CollectionAssert.AreEqual(data, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public async Task CopyToAsync_Stream_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var data = new byte[] { 1, 2, 3, 4, 5 };
            writer.Write(data);
            using var destination = new MemoryStream();

            // Act
            await writer.CopyToAsync(destination, 1024, default);
            var result = destination.ToArray();

            // Assert
            CollectionAssert.AreEqual(data, result);
            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void UnsupportedStreamOperations_ThrowException()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();

            // Assert
            Assert.ThrowsExactly<NotSupportedException>(() => writer.Position = 0);
            Assert.ThrowsExactly<NotSupportedException>(() => { var p = writer.Position; });
            Assert.ThrowsExactly<NotSupportedException>(() => writer.Read(new byte[1], 0, 1));
            Assert.ThrowsExactly<NotSupportedException>(() => writer.Seek(0, SeekOrigin.Begin));
            Assert.ThrowsExactly<NotSupportedException>(() => writer.SetLength(0));
            Assert.IsFalse(writer.CanRead);
            Assert.IsFalse(writer.CanSeek);

            MemoryBufferWriter.Return(writer);
        }

        [TestMethod]
        public void GetSpan_And_Advance_Success()
        {
            // Arrange
            var writer = MemoryBufferWriter.Get();
            var span = writer.GetSpan(5);
            var data = new byte[] { 1, 2, 3, 4, 5 };
            data.AsSpan().CopyTo(span);

            // Act
            writer.Advance(5);
            var result = writer.ToArray();

            // Assert
            CollectionAssert.AreEqual(data, result);
            MemoryBufferWriter.Return(writer);
        }
    }
}