using Raido.Common.Buffers;
using Raido.Server.Extensions;

namespace Raido.Server.Tests
{
    [TestClass]
    public class ByteBufferWriterTest
    {
        private MemoryBufferWriter _buffer = default!;
        
        [TestInitialize]
        public void Initialize()
        {
            _buffer = MemoryBufferWriter.Get();
        }
        
        [TestMethod]
        public void TestWriteString_WithStartDelimiter()
        {
            // Arrange
            const string inputString = "Hello, world!";

            // Act
            _buffer.WriteString(inputString, true);

            // Assert
            byte[] expectedBytes = new byte[] { 0, 72, 101, 108, 108, 111, 44, 32, 119, 111, 114, 108, 100, 33, 0 };
            byte[] actualBytes = _buffer.ToArray();
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }
        
        [TestMethod]
        public void TestWriteString_WithoutStartDelimiter()
        {
            // Arrange
            const string inputString = "Hello, world!";

            // Act
            _buffer.WriteString(inputString);

            // Assert
            byte[] expectedBytes = new byte[] { 72, 101, 108, 108, 111, 44, 32, 119, 111, 114, 108, 100, 33, 0 };
            byte[] actualBytes = _buffer.ToArray();
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }
        
        [TestMethod]
        public void TestWriteString_WithEmptyString()
        {
            // Arrange
            const string inputString = "";

            // Act
            _buffer.WriteString(inputString);

            // Assert
            byte[] expectedBytes = new byte[] { 0 };
            byte[] actualBytes = _buffer.ToArray();
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }
        [TestMethod]
        public void TestWriteString_WithLongString()
        {
            // Arrange
            string inputString = new string('a', 1000);

            // Act
            _buffer.WriteString(inputString);

            // Assert
            byte[] expectedBytes = new byte[1001];
            for (int i = 0; i < 1000; i++)
            {
                expectedBytes[i] = 97; // ASCII value for 'a'
            }
            expectedBytes[1000] = 0; // end delimiter
            byte[] actualBytes = _buffer.ToArray();
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }
    }
}