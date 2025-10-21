using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raido.Common.Buffers;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// Represents a binary writer for Raido messages.
    /// </summary>
    public interface IRaidoMessageBinaryWriter : IByteBufferWriter, IRaidoMessageBitWriterBegin
    {
        /// <summary>
        /// Sets the opcode of the message.
        /// </summary>
        /// <param name="opcode">The opcode of the message.</param>
        /// <returns>The current instance of the <see cref="IRaidoMessageBinaryWriter"/>.</returns>
        IRaidoMessageBinaryWriter SetOpcode(int opcode);

        /// <summary>
        /// Sets the size of the message.
        /// </summary>
        /// <param name="size">The size of the message.</param>
        /// <returns>The current instance of the <see cref="IRaidoMessageBinaryWriter"/>.</returns>
        IRaidoMessageBinaryWriter SetSize(RaidoMessageSize size);
    }
}
