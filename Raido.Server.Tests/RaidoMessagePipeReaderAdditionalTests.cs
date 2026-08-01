using System.Buffers;
using System.IO.Pipelines;
using NSubstitute;
using Raido.Common.Protocol;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoMessagePipeReaderAdditionalTests
{
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
