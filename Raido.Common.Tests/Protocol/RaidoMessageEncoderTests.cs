using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Buffers;
using Raido.Common.Protocol;
using System;
using System.Buffers;

namespace Raido.Common.Tests.Protocol
{
    [TestClass]
    public class RaidoMessageEncoderTests
    {
        [TestMethod]
        public void Encode_WritesToBuffer()
        {
            // Arrange
            var encoder = new TestEncoder();
            var buffer = MemoryBufferWriter.Get();
            var writer = new RaidoMessageBinaryWriter(buffer);
            var message = new TestMessage("test");

            // Act
            encoder.EncodeMessage(message, writer);

            // Assert
            var result = buffer.ToArray();
            Assert.AreEqual(4, result.Length);
            CollectionAssert.AreEqual(new byte[] { 116, 101, 115, 116 }, result);
            MemoryBufferWriter.Return(buffer);
        }

        private class TestMessage : RaidoMessage
        {
            public string Value { get; }

            public TestMessage(string value)
            {
                Value = value;
            }
        }

        private class TestEncoder : IRaidoMessageEncoder<TestMessage>
        {
            public void EncodeMessage(TestMessage message, IRaidoMessageBinaryWriter writer)
            {
                var span = writer.GetSpan(message.Value.Length);
                for (int i = 0; i < message.Value.Length; i++)
                {
                    span[i] = (byte)message.Value[i];
                }
                writer.Advance(message.Value.Length);
            }
        }
    }
}
