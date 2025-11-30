using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Protocol;

namespace Raido.Server.Tests
{
    [TestClass]
    public class RaidoMessagePipeReaderTests
    {
        [TestMethod]
        public void Constructor_NullReader_ThrowsArgumentNullException()
        {
            // Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new RaidoMessagePipeReader(null, Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>()));
        }

        [TestMethod]
        public void Constructor_NullMessageReader_ThrowsArgumentNullException()
        {
            // Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new RaidoMessagePipeReader(Substitute.For<PipeReader>(), null));
        }

        [TestMethod]
        public void TryRead_SuccessfulParse_ReturnsTrue()
        {
            // Arrange
            var pipeReader = Substitute.For<PipeReader>();
            var messageReader = Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>();
            var reader = new RaidoMessagePipeReader(pipeReader, messageReader);
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 });
            var readResult = new ReadResult(buffer, false, false);
            pipeReader.TryRead(out Arg.Any<ReadResult>()).Returns(x => { x[0] = readResult; return true; });
            messageReader.TryParseMessage(Arg.Any<ReadOnlySequence<byte>>(), ref Arg.Any<SequencePosition>(), ref Arg.Any<SequencePosition>(), out Arg.Any<ReadOnlySequence<byte>>()).Returns(x => { x[3] = buffer; return true; });

            // Act
            var result = reader.TryRead(out var message);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(buffer.Length, message.Buffer.Length);
        }

        [TestMethod]
        public void TryRead_IncompleteParse_ReturnsTrueWithBacklog()
        {
            // Arrange
            var pipeReader = Substitute.For<PipeReader>();
            var messageReader = Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>();
            var reader = new RaidoMessagePipeReader(pipeReader, messageReader);
            var buffer = new ReadOnlySequence<byte>([1, 2, 3]);
            var readResult = new ReadResult(buffer, false, false);
            pipeReader.TryRead(out Arg.Any<ReadResult>()).Returns(x => { x[0] = readResult; return true; });
            messageReader.TryParseMessage(Arg.Any<ReadOnlySequence<byte>>(), ref Arg.Any<SequencePosition>(), ref Arg.Any<SequencePosition>(), out Arg.Any<ReadOnlySequence<byte>>()).Returns(false);

            // Act
            var result = reader.TryRead(out var message);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, message.Buffer.Length);
        }

        [TestMethod]
        public async Task ReadAsync_SuccessfulParse_ReturnsBuffer()
        {
            // Arrange
            var pipeReader = Substitute.For<PipeReader>();
            var messageReader = Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>();
            var reader = new RaidoMessagePipeReader(pipeReader, messageReader);
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 });
            var readResult = new ReadResult(buffer, false, false);
            pipeReader.ReadAsync(default).Returns(readResult);
            messageReader.TryParseMessage(Arg.Any<ReadOnlySequence<byte>>(), ref Arg.Any<SequencePosition>(), ref Arg.Any<SequencePosition>(), out Arg.Any<ReadOnlySequence<byte>>()).Returns(x => { x[3] = buffer; return true; });

            // Act
            var result = await reader.ReadAsync();

            // Assert
            Assert.AreEqual(buffer.Length, result.Buffer.Length);
        }
    }
}
