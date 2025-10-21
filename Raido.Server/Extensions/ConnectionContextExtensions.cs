using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Raido.Common.Protocol;

namespace Raido.Server.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ConnectionContext"/>.
    /// </summary>
    public static class ConnectionContextExtensions
    {
        /// <summary>
        /// Creates a new <see cref="RaidoProtocolWriter"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A new <see cref="RaidoProtocolWriter"/>.</returns>
        public static RaidoProtocolWriter CreateWriter(this ConnectionContext connection)
            => new(connection.Transport.Output);

        /// <summary>
        /// Creates a new <see cref="RaidoProtocolWriter"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="semaphore">The semaphore to use for synchronization.</param>
        /// <returns>A new <see cref="RaidoProtocolWriter"/>.</returns>
        public static RaidoProtocolWriter CreateWriter(this ConnectionContext connection, SemaphoreSlim semaphore)
            => new(connection.Transport.Output, semaphore);

        /// <summary>
        /// Creates a new <see cref="RaidoProtocolReader"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A new <see cref="RaidoProtocolReader"/>.</returns>
        public static RaidoProtocolReader CreateReader(this ConnectionContext connection)
            => new(connection.Transport.Input);

        /// <summary>
        /// Creates a new <see cref="PipeReader"/> for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="raidoMessageReader">The message reader.</param>
        /// <returns>A new <see cref="PipeReader"/>.</returns>
        public static PipeReader CreatePipeReader(this ConnectionContext connection, IRaidoMessageReader<ReadOnlySequence<byte>> raidoMessageReader)
            => new RaidoMessagePipeReader(connection.Transport.Input, raidoMessageReader);
    }
}