using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Buffers;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoProtocolAndExtensionsTests
{
    private sealed class TestHub : RaidoHub { }

    private sealed class MessageWriter : IRaidoMessageWriter<TestMessage>
    {
        public void WriteMessage(TestMessage message, IBufferWriter<byte> output)
        {
            output.Write(new byte[] { 1, 2, 3 });
        }
    }

    private sealed class MessageReader : IRaidoMessageReader<TestMessage>
    {
        public bool Parse { get; set; } = true;
        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out TestMessage? message)
        {
            examined = input.End;
            if (!Parse || input.Length == 0)
            {
                message = null;
                return false;
            }
            consumed = input.End;
            message = new TestMessage();
            return true;
        }
    }

    private sealed class ReaderThatParsesAfterFirstAttempt : IRaidoMessageReader<TestMessage>
    {
        private int _attempts;

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out TestMessage? message)
        {
            examined = input.End;
            if (++_attempts == 1)
            {
                message = null;
                return false;
            }

            consumed = input.End;
            message = new TestMessage();
            return true;
        }
    }

    private sealed class ReaderThatLeavesTrailingBytes : IRaidoMessageReader<TestMessage>
    {
        private bool _parsed;

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out TestMessage? message)
        {
            examined = input.End;
            if (_parsed || input.Length == 0)
            {
                message = null;
                return false;
            }

            _parsed = true;
            consumed = input.GetPosition(1);
            message = new TestMessage();
            return true;
        }
    }

    private sealed class TwoByteMessageReader : IRaidoMessageReader<TestMessage>
    {
        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out TestMessage? message)
        {
            examined = input.End;
            if (input.Length < 2)
            {
                message = null;
                return false;
            }

            consumed = input.GetPosition(2);
            message = new TestMessage();
            return true;
        }
    }

    private sealed class NoopMessageWriter : IRaidoMessageWriter<TestMessage>
    {
        public void WriteMessage(TestMessage message, IBufferWriter<byte> output) { }
    }

    private sealed class TestDisposable : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private sealed class TestAsyncDisposable : IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private static ReadOnlySequence<byte> Bytes(params byte[] bytes) => new(bytes);

    [TestMethod]
    public void BufferExtensions_WriteBigEndianIntegers()
    {
        using var buffer = MemoryBufferWriter.Get();
        Assert.AreSame(buffer, buffer.WriteInt16BigEndian(unchecked((short)0x1234)));
        buffer.WriteInt24BigEndian(0x56789A);
        buffer.WriteInt32BigEndian(unchecked((int)0xBCDEF012));
        buffer.WriteInt40BigEndian(0x3456789ABC);
        buffer.WriteInt64BigEndian(unchecked((long)0x123456789ABCDEF0));
        CollectionAssert.AreEqual(
            new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 },
            buffer.ToArray());
    }

    [TestMethod]
    public void SequenceReaderExtensions_ReadsDelimitedStringsAndBooleans()
    {
        var reader = new SequenceReader<byte>(Bytes(0, (byte)'h', (byte)'i', 0, 1, 0));
        Assert.IsTrue(reader.TryRead(out var value, readStartDelimiter: true));
        Assert.AreEqual("hi", value);
        Assert.IsTrue(reader.TryRead(out bool enabled));
        Assert.IsTrue(enabled);
        Assert.IsTrue(reader.TryRead(out bool disabled));
        Assert.IsFalse(disabled);

        var missingStart = new SequenceReader<byte>(Bytes(1, 2));
        Assert.IsFalse(missingStart.TryRead(out _, readStartDelimiter: true));
        var empty = new SequenceReader<byte>(ReadOnlySequence<byte>.Empty);
        Assert.IsFalse(empty.TryRead(out bool emptyBool));
        Assert.IsFalse(emptyBool);
    }

    [TestMethod]
    public async Task ProtocolWriter_WritesOneAndManyMessagesAndStopsAfterCompletion()
    {
        var pipe = new Pipe();
        await using var writer = new RaidoProtocolWriter(pipe.Writer);
        var messageWriter = new MessageWriter();
        await writer.WriteAsync(messageWriter, new TestMessage());
        await writer.WriteManyAsync(messageWriter, new[] { new TestMessage(), new TestMessage() });
        var result = await pipe.Reader.ReadAsync();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 }, result.Buffer.ToArray());
        pipe.Reader.AdvanceTo(result.Buffer.End);
        await writer.DisposeAsync();
        await writer.WriteAsync(messageWriter, new TestMessage());
        await pipe.Reader.CompleteAsync();
    }

    [TestMethod]
    public async Task ProtocolWriter_HandlesCanceledAndCompletedFlushes()
    {
        var pipeWriter = Substitute.For<PipeWriter>();
        pipeWriter.FlushAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<FlushResult>(new FlushResult(true, false)));
        await using (var writer = new RaidoProtocolWriter(pipeWriter))
        {
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await writer.WriteAsync(new NoopMessageWriter(), new TestMessage()));
        }

        pipeWriter = Substitute.For<PipeWriter>();
        pipeWriter.FlushAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<FlushResult>(new FlushResult(false, true)));
        var completedWriter = new RaidoProtocolWriter(pipeWriter);
        await completedWriter.WriteAsync(new NoopMessageWriter(), new TestMessage());
        await completedWriter.WriteAsync(new NoopMessageWriter(), new TestMessage());
        await completedWriter.DisposeAsync();
    }

    [TestMethod]
    public async Task ProtocolWriter_WriteManyHandlesCanceledAndCompletedFlushes()
    {
        var pipeWriter = Substitute.For<PipeWriter>();
        pipeWriter.FlushAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<FlushResult>(new FlushResult(true, false)));
        await using (var writer = new RaidoProtocolWriter(pipeWriter))
        {
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
                writer.WriteManyAsync(new NoopMessageWriter(), new[] { new TestMessage() }).AsTask());
        }

        pipeWriter = Substitute.For<PipeWriter>();
        pipeWriter.FlushAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<FlushResult>(new FlushResult(false, true)));
        await using var completedWriter = new RaidoProtocolWriter(pipeWriter);
        await completedWriter.WriteManyAsync(new NoopMessageWriter(), new[] { new TestMessage() });
        await completedWriter.WriteManyAsync(new NoopMessageWriter(), new[] { new TestMessage() });
    }

    [TestMethod]
    public async Task ProtocolReader_ParsesMessagesAdvancesAndCompletes()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 1, 2 });
        await using var reader = new RaidoProtocolReader(pipe.Reader);
        var messageReader = new MessageReader();
        var result = await reader.ReadAsync(messageReader);
        Assert.IsInstanceOfType<TestMessage>(result.Message);
        Assert.IsFalse(result.IsCompleted);
        Assert.ThrowsExactly<InvalidOperationException>(() => reader.ReadAsync(messageReader).AsTask().GetAwaiter().GetResult());
        reader.Advance();
        await pipe.Writer.CompleteAsync();
        var completed = await reader.ReadAsync(messageReader);
        Assert.IsTrue(completed.IsCompleted);
        Assert.IsNull(completed.Message);
    }

    [TestMethod]
    public async Task ProtocolReader_HandlesIncompleteAndMaximumSize()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 1, 2, 3 });
        await using var reader = new RaidoProtocolReader(pipe.Reader);
        var messageReader = new MessageReader { Parse = false };
        var pending = reader.ReadAsync(messageReader).AsTask();
        Assert.IsFalse(pending.IsCompleted);
        await pipe.Writer.WriteAsync(new byte[] { 4 });
        messageReader.Parse = true;
        var result = await pending;
        Assert.IsInstanceOfType<TestMessage>(result.Message);
        reader.Advance();

        var limitedPipe = new Pipe();
        await limitedPipe.Writer.WriteAsync(new byte[] { 1, 2, 3 });
        await using var limitedReader = new RaidoProtocolReader(limitedPipe.Reader);
        var neverParses = new MessageReader { Parse = false };
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => limitedReader.ReadAsync(neverParses, 2).AsTask());
        await limitedPipe.Reader.CompleteAsync();
    }

    [TestMethod]
    public async Task ProtocolReader_AwaitsAnIncompleteUnderlyingRead()
    {
        var underlying = Substitute.For<PipeReader>();
        var pendingRead = new TaskCompletionSource<ReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<ReadResult>(pendingRead.Task));
        await using var reader = new RaidoProtocolReader(underlying);

        var pending = reader.ReadAsync(new MessageReader()).AsTask();
        Assert.IsFalse(pending.IsCompleted);

        pendingRead.SetResult(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1 }), false, false));
        var result = await pending;

        Assert.IsInstanceOfType<TestMessage>(result.Message);
        reader.Advance();
    }

    [TestMethod]
    public async Task ProtocolReader_ContinuesAfterAnIncompleteUnparsedRead()
    {
        var underlying = Substitute.For<PipeReader>();
        var pendingRead = new TaskCompletionSource<ReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(pendingRead.Task),
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1 }), false, false)));
        await using var reader = new RaidoProtocolReader(underlying);

        var pending = reader.ReadAsync(new ReaderThatParsesAfterFirstAttempt()).AsTask();
        pendingRead.SetResult(new ReadResult(ReadOnlySequence<byte>.Empty, false, false));
        var result = await pending;

        Assert.IsInstanceOfType<TestMessage>(result.Message);
        reader.Advance();
    }

    [TestMethod]
    public async Task ProtocolReader_PropagatesCanceledUnderlyingRead()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(ReadOnlySequence<byte>.Empty, isCanceled: true, isCompleted: false)));
        await using var reader = new RaidoProtocolReader(underlying);

        var result = await reader.ReadAsync(new MessageReader());

        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(result.IsCompleted);
        reader.Advance();
    }

    [TestMethod]
    public async Task ProtocolReader_PreservesCompleteMessageBeforeCanceledBoundary()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 }), isCanceled: true, isCompleted: false)));
        await using var reader = new RaidoProtocolReader(underlying);
        var messageReader = new TwoByteMessageReader();

        var message = await reader.ReadAsync(messageReader);

        Assert.IsNotNull(message.Message);
        Assert.IsFalse(message.IsCanceled);
        reader.Advance();

        var canceled = await reader.ReadAsync(messageReader);

        Assert.IsTrue(canceled.IsCanceled);
        reader.Advance(advanceCursor: true);
    }

    [TestMethod]
    public async Task ProtocolReader_RejectsUnparseableCompletedInput()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1 }), false, true)));
        await using var reader = new RaidoProtocolReader(underlying);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
            reader.ReadAsync(new MessageReader { Parse = false }).AsTask());
    }

    [TestMethod]
    public async Task ProtocolReader_RejectsTrailingBytesAfterCompletedMessage()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1, 2 }), false, true)));
        await using var reader = new RaidoProtocolReader(underlying);
        var messageReader = new ReaderThatLeavesTrailingBytes();

        var message = await reader.ReadAsync(messageReader);
        Assert.IsInstanceOfType<TestMessage>(message.Message);
        reader.Advance();

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => reader.ReadAsync(messageReader).AsTask());
    }

    [TestMethod]
    public async Task ProtocolReader_ResumesAfterCanceledRead()
    {
        var underlying = Substitute.For<PipeReader>();
        underlying.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<ReadResult>(new ReadResult(ReadOnlySequence<byte>.Empty, true, false)),
            new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[] { 1 }), false, false)));
        await using var reader = new RaidoProtocolReader(underlying);

        var canceled = await reader.ReadAsync(new MessageReader());
        Assert.IsTrue(canceled.IsCanceled);
        reader.Advance();

        var message = await reader.ReadAsync(new MessageReader());
        Assert.IsInstanceOfType<TestMessage>(message.Message);
        reader.Advance();
    }

    [TestMethod]
    public async Task ProtocolReader_AllowsMessageAtTheMaximumSize()
    {
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(new byte[] { 1, 2 });
        await using var reader = new RaidoProtocolReader(pipe.Reader);

        var result = await reader.ReadAsync(new MessageReader(), maximumMessageSize: 2);

        Assert.IsInstanceOfType<TestMessage>(result.Message);
        reader.Advance();
        await pipe.Reader.CompleteAsync();
    }

    [TestMethod]
    public async Task ProtocolReader_RejectsUseAfterDisposeAndSupportsStreamConstructor()
    {
        var reader = new RaidoProtocolReader(new MemoryStream());
        await reader.DisposeAsync();
        Assert.ThrowsExactly<ObjectDisposedException>(() => reader.ReadAsync(new MessageReader()).AsTask().GetAwaiter().GetResult());
        Assert.ThrowsExactly<ObjectDisposedException>(() => reader.Advance());

        await using var writer = new RaidoProtocolWriter(new MemoryStream());
        await writer.DisposeAsync();
    }

    [TestMethod]
    public async Task DisposableExtension_UsesTheAvailableDisposalContract()
    {
        var sync = new TestDisposable();
        await sync.DisposeAsync();
        Assert.IsTrue(sync.Disposed);
        var asyncDisposable = new TestAsyncDisposable();
        await asyncDisposable.DisposeAsync();
        Assert.IsTrue(asyncDisposable.Disposed);
    }

    [TestMethod]
    public void OptionsAndContextExtensions_AddFiltersAndItems()
    {
        var options = new RaidoOptions();
        var filter = Substitute.For<IRaidoHubFilter>();
        options.AddGlobalFilter(filter);
        options.AddGlobalFilter<NoopFilter>();
        options.AddGlobalFilter(typeof(NoopFilter));
        Assert.AreEqual(3, options.GlobalHubFilters!.Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => options.AddGlobalFilter((IRaidoHubFilter)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => options.AddGlobalFilter((Type)null!));

        var hubOptions = new RaidoHubOptions<TestHub>();
        hubOptions.AddFilter(filter);
        hubOptions.AddFilter<TestHub, NoopFilter>();
        hubOptions.AddFilter(typeof(NoopFilter));
        Assert.AreEqual(3, hubOptions.HubFilters!.Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => hubOptions.AddFilter((IRaidoHubFilter)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => hubOptions.AddFilter((Type)null!));

        var context = Substitute.For<RaidoCallerContext>();
        context.Items.Returns(new Dictionary<object, object?> { ["number"] = 42, ["text"] = "x" });
        Assert.IsTrue(context.TryGetItem<int>("number", out var number));
        Assert.AreEqual(42, number);
        Assert.IsFalse(context.TryGetItem<int>("text", out _));
        Assert.IsFalse(context.TryGetItem<int>("missing", out _));
    }

    private sealed class NoopFilter : IRaidoHubFilter { }
}
