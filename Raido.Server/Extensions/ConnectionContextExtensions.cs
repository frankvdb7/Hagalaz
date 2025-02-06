using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Raido.Common.Protocol;

namespace Raido.Server.Extensions
{
    public static class ConnectionContextExtensions
    {
        public static RaidoProtocolWriter CreateWriter(this ConnectionContext connection)
            => new(connection.Transport.Output);

        public static RaidoProtocolWriter CreateWriter(this ConnectionContext connection, SemaphoreSlim semaphore)
            => new(connection.Transport.Output, semaphore);

        public static RaidoProtocolReader CreateReader(this ConnectionContext connection)
            => new(connection.Transport.Input);

        public static PipeReader CreatePipeReader(this ConnectionContext connection, IRaidoMessageReader<ReadOnlySequence<byte>> raidoMessageReader)
            => new RaidoMessagePipeReader(connection.Transport.Input, raidoMessageReader);
    }
}