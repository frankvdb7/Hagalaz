using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public class RaidoProtocolWriter : IAsyncDisposable
    {
        private readonly PipeWriter _writer;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public RaidoProtocolWriter(Stream stream) : 
            this(PipeWriter.Create(stream))
        {

        }

        public RaidoProtocolWriter(PipeWriter writer)
            : this(writer, new SemaphoreSlim(1))
        {
        }

        public RaidoProtocolWriter(PipeWriter writer, SemaphoreSlim semaphore)
        {
            _writer = writer;
            _semaphore = semaphore;
        }

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