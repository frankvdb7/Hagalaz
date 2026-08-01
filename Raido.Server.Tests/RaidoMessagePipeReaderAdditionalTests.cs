using System.Buffers;
using System.IO.Pipelines;
using NSubstitute;
using Raido.Common.Protocol;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoMessagePipeReaderAdditionalTests
{
    private sealed class OneByteMessageReader : IRaidoMessageReader<ReadOnlySequence<byte>>
    {
        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out ReadOnlySequence<byte> message)
        {
            if (input.IsEmpty)
            {
                message = default;
                return false;
            }

            consumed = input.GetPosition(1);
            examined = input.End;
            message = input.Slice(input.Start, 1);
            return true;
        }
    }

    [TestMethod]
    public async Task ReadAsync_DoesNotReportCompletionBeforeCoalescedMessagesAreConsumed()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1, 2 }), false, true)),
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 2 }), false, true)));
        var reader = new RaidoMessagePipeReader(underlying, new OneByteMessageReader());

        var first = await reader.ReadAsync();
        Assert.AreEqual(1, first.Buffer.Length);
        Assert.IsFalse(first.IsCompleted);
        reader.AdvanceTo(first.Buffer.End);

        var second = await reader.ReadAsync();
        Assert.AreEqual(1, second.Buffer.Length);
        Assert.IsTrue(second.IsCompleted);
        reader.AdvanceTo(second.Buffer.End);
        reader.Complete();

        await underlying.Received(2).ReadAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ReadAsync_PreservesUnconsumedMessagesInBacklog()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1 }), false, false)),
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 2 }), false, true)));
        var reader = new RaidoMessagePipeReader(underlying, new OneByteMessageReader());

        var first = await reader.ReadAsync();
        reader.AdvanceTo(first.Buffer.Start, first.Buffer.End);

        var combined = await reader.ReadAsync();

        Assert.AreEqual(2, combined.Buffer.Length);
        Assert.IsFalse(combined.IsCompleted);
        reader.AdvanceTo(combined.Buffer.End);
        reader.Complete();
    }

    [TestMethod]
    public void TryRead_ReturnsFalseAfterCompletedBufferWasFullyExamined()
    {
        var underlying = Substitute.For<PipeReader>();
        var bytes = new ReadOnlySequence<byte>(new byte[] { 1 });
        underlying.TryRead(out Arg.Any<ReadResult>()).Returns(x =>
        {
            x[0] = new ReadResult(bytes, false, true);
            return true;
        });
        var reader = new RaidoMessagePipeReader(underlying, new OneByteMessageReader());

        Assert.IsTrue(reader.TryRead(out var result));
        Assert.IsTrue(result.IsCompleted);
        reader.AdvanceTo(result.Buffer.End);

        Assert.IsFalse(reader.TryRead(out _));
        reader.Complete();
    }

    [TestMethod]
    public void TryRead_ReturnsUnexaminedCompletedBacklog()
    {
        var underlying = Substitute.For<PipeReader>();
        var bytes = new ReadOnlySequence<byte>(new byte[] { 1 });
        underlying.TryRead(out Arg.Any<ReadResult>()).Returns(x =>
        {
            x[0] = new ReadResult(bytes, false, true);
            return true;
        });
        var reader = new RaidoMessagePipeReader(underlying, new OneByteMessageReader());

        Assert.IsTrue(reader.TryRead(out var result));
        reader.AdvanceTo(result.Buffer.Start, result.Buffer.Start);

        Assert.IsTrue(reader.TryRead(out var backlog));
        Assert.AreEqual(1, backlog.Buffer.Length);
        Assert.IsTrue(backlog.IsCompleted);
        reader.AdvanceTo(backlog.Buffer.End);
        reader.Complete();
    }

    [TestMethod]
    public async Task ReadAsync_HandlesCanceledAndCompletedReads()
    {
        var underlying = Substitute.For<PipeReader>();
        var parser = Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(default, true, false)),
            new ValueTask<ReadResult>(new ReadResult(ReadOnlySequence<byte>.Empty, false, true)));
        var reader = new RaidoMessagePipeReader(underlying, parser);
        var canceled = await reader.ReadAsync();
        Assert.IsTrue(canceled.IsCanceled);
        var completed = await reader.ReadAsync();
        Assert.IsTrue(completed.IsCompleted);
        reader.AdvanceTo(completed.Buffer.End);
        reader.Complete();
        Assert.ThrowsExactly<InvalidOperationException>(() => reader.TryRead(out _));
    }

    [TestMethod]
    public async Task TryRead_ReportsBacklogAndCanBeAdvancedOrCanceled()
    {
        var underlying = Substitute.For<PipeReader>();
        var parser = Substitute.For<IRaidoMessageReader<ReadOnlySequence<byte>>>();
        var bytes = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 });
        underlying.TryRead(out Arg.Any<ReadResult>()).Returns(x =>
        {
            x[0] = new ReadResult(bytes, false, false);
            return true;
        });
        parser.TryParseMessage(Arg.Any<ReadOnlySequence<byte>>(), ref Arg.Any<SequencePosition>(), ref Arg.Any<SequencePosition>(), out Arg.Any<ReadOnlySequence<byte>>())
            .Returns(x => { x[3] = new ReadOnlySequence<byte>(new byte[] { 1 }); return true; });
        var reader = new RaidoMessagePipeReader(underlying, parser);
        Assert.IsTrue(reader.TryRead(out var result));
        Assert.AreEqual(1, result.Buffer.Length);
        reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        reader.CancelPendingRead();
        reader.Complete();
        Assert.ThrowsExactly<InvalidOperationException>(() => reader.AdvanceTo(result.Buffer.End));
    }
}
