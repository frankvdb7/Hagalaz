using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoEdgeCaseTests
{
    private sealed class CountingMessageWriter : IRaidoMessageWriter<TestMessage>
    {
        public int Calls { get; private set; }

        public void WriteMessage(TestMessage message, IBufferWriter<byte> output) => Calls++;
    }

    private sealed class EmptyMessageReader : IRaidoMessageReader<ReadOnlySequence<byte>>
    {
        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out ReadOnlySequence<byte> message)
        {
            consumed = input.End;
            examined = input.End;
            message = ReadOnlySequence<byte>.Empty;
            return true;
        }
    }

    private sealed class NeverMessageReader : IRaidoMessageReader<ReadOnlySequence<byte>>
    {
        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out ReadOnlySequence<byte> message)
        {
            consumed = input.Start;
            examined = input.End;
            message = default;
            return false;
        }
    }

    [TestMethod]
    public void ConsumableBuffer_ShiftsConsumedPrefixBeforeGrowing()
    {
        using var buffer = new ConsumableArrayBufferWriter(32);
        var initial = buffer.GetSpan(20)[..20];
        for (var i = 0; i < initial.Length; i++)
        {
            initial[i] = (byte)i;
        }

        buffer.Advance(initial.Length);
        buffer.Consume(15);

        var appended = buffer.GetSpan(13)[..13];
        appended.Fill(42);
        buffer.Advance(appended.Length);

        var expected = new byte[18];
        for (var i = 0; i < 5; i++)
        {
            expected[i] = (byte)(i + 15);
        }

        Array.Fill(expected, (byte)42, 5, 13);
        CollectionAssert.AreEqual(expected, buffer.WrittenSpan.ToArray());
    }

    [TestMethod]
    public void ConsumableBuffer_ReleasesLargeArrayWhenEverythingIsConsumed()
    {
        using var buffer = new ConsumableArrayBufferWriter(512);
        buffer.GetSpan(1)[0] = 7;
        buffer.Advance(1);

        buffer.Consume(1);

        Assert.AreEqual(0, buffer.Capacity);
        Assert.AreEqual(0, buffer.UnconsumedWrittenCount);
    }

    [TestMethod]
    public void MessagePipeReader_EmptyMessageOnCompletedInputReturnsCompletedResult()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(ReadOnlySequence<byte>.Empty, isCanceled: false, isCompleted: true)));
        var reader = new RaidoMessagePipeReader(underlying, new EmptyMessageReader());

        var result = reader.ReadAsync().AsTask().GetAwaiter().GetResult();

        Assert.IsTrue(result.IsCompleted);
        Assert.IsTrue(result.Buffer.IsEmpty);
        reader.Complete();
        underlying.Received(1).AdvanceTo(Arg.Any<SequencePosition>(), Arg.Any<SequencePosition>());
    }

    [TestMethod]
    public void MessagePipeReader_ReadAfterCompleteThrowsEvenWithoutAnUnderlyingRead()
    {
        var reader = new RaidoMessagePipeReader(
            Substitute.For<PipeReader>(),
            Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>());

        reader.Complete();

        Assert.ThrowsExactly<InvalidOperationException>(() => reader.ReadAsync().AsTask().GetAwaiter().GetResult());
    }

    [TestMethod]
    public void MessagePipeReader_IncompleteCompletedReadDoesNotPublishPartialMessage()
    {
        var underlying = Substitute.For<PipeReader>();
        var bytes = new ReadOnlySequence<byte>(new byte[] { 1, 2 });
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(bytes, isCanceled: false, isCompleted: true)));
        var reader = new RaidoMessagePipeReader(underlying, new NeverMessageReader());

        Assert.ThrowsExactly<InvalidDataException>(() => reader.ReadAsync().AsTask().GetAwaiter().GetResult());
        reader.Complete();
    }

    [TestMethod]
    public void ProtocolWriter_CanceledBeforeSemaphoreAcquisitionDoesNotInvokeMessageWriter()
    {
        var writer = new RaidoProtocolWriter(Substitute.For<PipeWriter>());
        var messageWriter = new CountingMessageWriter();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsExactly<TaskCanceledException>(() =>
            writer.WriteAsync(messageWriter, new TestMessage(), cancellation.Token).AsTask().GetAwaiter().GetResult());

        Assert.AreEqual(0, messageWriter.Calls);
        writer.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
