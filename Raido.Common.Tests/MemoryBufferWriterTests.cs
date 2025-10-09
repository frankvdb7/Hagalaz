using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Buffers;
using System;

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
    }
}