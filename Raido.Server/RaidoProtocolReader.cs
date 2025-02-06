using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public class RaidoProtocolReader : IAsyncDisposable
    {
        private readonly PipeReader _reader;
        private SequencePosition _examined;
        private SequencePosition _consumed;
        private ReadOnlySequence<byte> _buffer;
        private bool _isCanceled;
        private bool _isCompleted;
        private bool _hasMessage;
        private bool _disposed;

        public RaidoProtocolReader(Stream stream) : this(PipeReader.Create(stream)) { }

        public RaidoProtocolReader(PipeReader reader) => _reader = reader;

        public ValueTask<RaidoProtocolReadResult<TReadMessage>> ReadAsync<TReadMessage>(
            IRaidoMessageReader<TReadMessage> reader,
            CancellationToken cancellationToken = default) =>
            ReadAsync(reader, maximumMessageSize: null, cancellationToken);

        public ValueTask<RaidoProtocolReadResult<TReadMessage>> ReadAsync<TReadMessage>(
            IRaidoMessageReader<TReadMessage> reader,
            long maximumMessageSize,
            CancellationToken cancellationToken = default) =>
            ReadAsync(reader, (long?)maximumMessageSize, cancellationToken);

        public ValueTask<RaidoProtocolReadResult<TReadMessage>> ReadAsync<TReadMessage>(
            IRaidoMessageReader<TReadMessage> reader,
            long? maximumMessageSize,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RaidoProtocolReader));
            }

            if (_hasMessage)
            {
                throw new InvalidOperationException($"{nameof(Advance)} must be called before calling {nameof(ReadAsync)}");
            }

            // If this is the very first read, then make it go async since we have no data
            if (_consumed.GetObject() == null)
            {
                return DoAsyncRead(maximumMessageSize, reader, cancellationToken);
            }

            // We have a buffer, test to see if there's any message left in the buffer
            if (TryParseMessage(maximumMessageSize, reader, _buffer, out var protocolMessage))
            {
                _hasMessage = true;
                return new ValueTask<RaidoProtocolReadResult<TReadMessage>>(
                    new RaidoProtocolReadResult<TReadMessage>(protocolMessage, _isCanceled, isCompleted: false));
            }

            // We couldn't parse the message so advance the input so we can read
            _reader.AdvanceTo(_consumed, _examined);

            // Reset the state since we're done consuming this buffer
            _buffer = default;
            _consumed = default;
            _examined = default;

            if (!_isCompleted)
            {
                return DoAsyncRead(maximumMessageSize, reader, cancellationToken);
            }

            _consumed = default;
            _examined = default;

            // If we're complete then short-circuit
            if (!_buffer.IsEmpty)
            {
                throw new InvalidDataException("Connection terminated while reading a message.");
            }

            return new ValueTask<RaidoProtocolReadResult<TReadMessage>>(new RaidoProtocolReadResult<TReadMessage>(default, _isCanceled, _isCompleted));
        }

        private ValueTask<RaidoProtocolReadResult<TReadMessage>> DoAsyncRead<TReadMessage>(
            long? maximumMessageSize,
            IRaidoMessageReader<TReadMessage> reader,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                var readTask = _reader.ReadAsync(cancellationToken);
                ReadResult result;
                if (readTask.IsCompletedSuccessfully)
                {
                    result = readTask.Result;
                }
                else
                {
                    return ContinueDoAsyncRead(readTask, maximumMessageSize, reader, cancellationToken);
                }

                var (shouldContinue, hasMessage) = TrySetMessage(result, maximumMessageSize, reader, out var protocolReadResult);
                if (hasMessage)
                {
                    return new ValueTask<RaidoProtocolReadResult<TReadMessage>>(protocolReadResult);
                }

                if (!shouldContinue)
                {
                    break;
                }
            }

            return new ValueTask<RaidoProtocolReadResult<TReadMessage>>(new RaidoProtocolReadResult<TReadMessage>(default, _isCanceled, _isCompleted));
        }

        private async ValueTask<RaidoProtocolReadResult<TReadMessage>> ContinueDoAsyncRead<TReadMessage>(
            ValueTask<ReadResult> readTask,
            long? maximumMessageSize,
            IRaidoMessageReader<TReadMessage> reader,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await readTask;

                var (shouldContinue, hasMessage) = TrySetMessage(result, maximumMessageSize, reader, out var protocolReadResult);
                if (hasMessage)
                {
                    return protocolReadResult;
                }

                if (!shouldContinue)
                {
                    break;
                }

                readTask = _reader.ReadAsync(cancellationToken);
            }

            return new RaidoProtocolReadResult<TReadMessage>(default, _isCanceled, _isCompleted);
        }

        private (bool ShouldContinue, bool HasMessage) TrySetMessage<TReadMessage>(
            ReadResult result,
            long? maximumMessageSize,
            IRaidoMessageReader<TReadMessage> reader,
            out RaidoProtocolReadResult<TReadMessage> readResult)
        {
            _buffer = result.Buffer;
            _isCanceled = result.IsCanceled;
            _isCompleted = result.IsCompleted;
            _consumed = _buffer.Start;
            _examined = _buffer.End;

            if (_isCanceled)
            {
                readResult = default;
                return (false, false);
            }

            if (TryParseMessage(maximumMessageSize, reader, _buffer, out var protocolMessage))
            {
                _hasMessage = true;
                readResult = new RaidoProtocolReadResult<TReadMessage>(protocolMessage, _isCanceled, isCompleted: false);
                return (false, true);
            }

            _reader.AdvanceTo(_consumed, _examined);

            // Reset the state since we're done consuming this buffer
            _buffer = default;
            _consumed = default;
            _examined = default;

            if (_isCompleted)
            {
                _consumed = default;
                _examined = default;

                if (!_buffer.IsEmpty)
                {
                    throw new InvalidDataException("Connection terminated while reading a message.");
                }

                readResult = default;
                return (false, false);
            }

            readResult = default;
            return (true, false);
        }

        private bool TryParseMessage<TReadMessage>(
            long? maximumMessageSize,
            IRaidoMessageReader<TReadMessage> reader,
            in ReadOnlySequence<byte> buffer,
            out TReadMessage? message)
        {
            if (_buffer.IsEmpty)
            {
                message = default;
                return false;
            }
            
            // No message limit, just parse and dispatch
            if (maximumMessageSize == null)
            {
                return reader.TryParseMessage(buffer, ref _consumed, ref _examined, out message);
            }

            // We give the parser a sliding window of the default message size
            var maxMessageSize = maximumMessageSize.Value;

            var segment = buffer;
            var overLength = false;

            if (segment.Length > maxMessageSize)
            {
                segment = segment.Slice(segment.Start, maxMessageSize);
                overLength = true;
            }

            if (reader.TryParseMessage(segment, ref _consumed, ref _examined, out message))
            {
                return true;
            }

            if (overLength)
            {
                throw new InvalidDataException($"The maximum message size of {maxMessageSize}B was exceeded.");
            }

            return false;
        }

        /// <summary>
        /// Advances the buffer based on the consumed sequence position.
        /// </summary>
        /// <param name="advanceCursor">If true, will advance the reader cursor to the consumed sequence position. By default: false</param>
        public void Advance(bool advanceCursor = false)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RaidoProtocolReader));
            }

            if (advanceCursor)
            {
                _reader.AdvanceTo(_consumed);
            }

            _isCanceled = false;

            if (!_hasMessage)
            {
                return;
            }

            _buffer = _buffer.Slice(_consumed);

            _hasMessage = false;
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return default;
        }
    }
}