using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A protocol writer for Raido messages.
    /// </summary>
    public class RaidoProtocolWriter : IAsyncDisposable
    {
        private readonly PipeWriter _writer;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoProtocolWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public RaidoProtocolWriter(Stream stream) : 
            this(PipeWriter.Create(stream))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoProtocolWriter"/> class.
        /// </summary>
        /// <param name="writer">The pipe writer to write to.</param>
        public RaidoProtocolWriter(PipeWriter writer)
            : this(writer, new SemaphoreSlim(1))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoProtocolWriter"/> class.
        /// </summary>
        /// <param name="writer">The pipe writer to write to.</param>
        /// <param name="semaphore">The semaphore to use for synchronization.</param>
        public RaidoProtocolWriter(PipeWriter writer, SemaphoreSlim semaphore)
        {
            _writer = writer;
            _semaphore = semaphore;
        }

        /// <summary>
        /// Writes a message to the protocol.
        /// </summary>
        /// <typeparam name="TWriteMessage">The type of the message to write.</typeparam>
        /// <param name="writer">The message writer.</param>
        /// <param name="protocolMessage">The message to write.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous write operation.</returns>
        public async ValueTask WriteAsync<TWriteMessage>(IRaidoMessageWriter<TWriteMessage> writer, TWriteMessage protocolMessage, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_disposed)
                {
                    return;
                }

                writer.WriteMessage(protocolMessage, _writer);

                var result = await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException();
                }

                if (result.IsCompleted)
                {
                    _disposed = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Writes multiple messages to the protocol.
        /// </summary>
        /// <typeparam name="TWriteMessage">The type of the messages to write.</typeparam>
        /// <param name="writer">The message writer.</param>
        /// <param name="protocolMessages">The messages to write.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous write operation.</returns>
        public async ValueTask WriteManyAsync<TWriteMessage>(IRaidoMessageWriter<TWriteMessage> writer, IEnumerable<TWriteMessage> protocolMessages, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_disposed)
                {
                    return;
                }

                foreach(var protocolMessage in protocolMessages)
                {
                    writer.WriteMessage(protocolMessage, _writer);
                }

                var result = await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException();
                }

                if (result.IsCompleted)
                {
                    _disposed = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Disposes the writer.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}