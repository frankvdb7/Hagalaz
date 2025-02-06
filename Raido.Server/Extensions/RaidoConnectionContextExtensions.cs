using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using Raido.Common.Protocol;

namespace Raido.Server.Extensions
{
    public static class RaidoConnectionContextExtensions
    {
        public static RaidoProtocolWriter CreateWriter(this RaidoConnectionContext connection)
            => new(connection.Output);

        public static RaidoProtocolWriter CreateWriter(this RaidoConnectionContext connection, SemaphoreSlim semaphore)
            => new(connection.Output, semaphore);

        public static RaidoProtocolReader CreateReader(this RaidoConnectionContext connection)
            => new(connection.Input);

        public static PipeReader CreatePipeReader(this RaidoConnectionContext connection, IRaidoMessageReader<ReadOnlySequence<byte>> raidoMessageReader)
            => new RaidoMessagePipeReader(connection.Input, raidoMessageReader);
    }
}