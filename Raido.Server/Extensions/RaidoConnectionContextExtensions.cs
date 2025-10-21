using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using Raido.Common.Protocol;

namespace Raido.Server.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="RaidoConnectionContext"/>.
    /// </summary>
    public static class RaidoConnectionContextExtensions
    {
        /// <summary>
        /// Creates a new <see cref="RaidoProtocolWriter"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A new <see cref="RaidoProtocolWriter"/>.</returns>
        public static RaidoProtocolWriter CreateWriter(this RaidoConnectionContext connection)
            => new(connection.Output);

        /// <summary>
        /// Creates a new <see cref="RaidoProtocolWriter"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="semaphore">The semaphore to use for synchronization.</param>
        /// <returns>A new <see cref="RaidoProtocolWriter"/>.</returns>
        public static RaidoProtocolWriter CreateWriter(this RaidoConnectionContext connection, SemaphoreSlim semaphore)
            => new(connection.Output, semaphore);

        /// <summary>
        /// Creates a new <see cref="RaidoProtocolReader"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A new <see cref="RaidoProtocolReader"/>.</returns>
        public static RaidoProtocolReader CreateReader(this RaidoConnectionContext connection)
            => new(connection.Input);

        /// <summary>
        /// Creates a new <see cref="PipeReader"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="raidoMessageReader">The message reader.</param>
        /// <returns>A new <see cref="PipeReader"/>.</returns>
        public static PipeReader CreatePipeReader(this RaidoConnectionContext connection, IRaidoMessageReader<ReadOnlySequence<byte>> raidoMessageReader)
            => new RaidoMessagePipeReader(connection.Input, raidoMessageReader);
    }
}