using System;
using System.Buffers;
using System.Buffers.Binary;
using Hagalaz.Services.GameUpdate.Network.Messages;
using Raido.Common.Buffers;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameUpdate.Network.Protocol
{
    public class HandshakeProtocol : IRaidoMessageReader<HandshakeRequest>, IRaidoMessageWriter<HandshakeResponse>
    {
        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out HandshakeRequest? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out byte opcode) || opcode != HandshakeRequest.Opcode)
            {
                message = default;
                return false;
            }

            if (!reader.TryRead(out byte length) || input.Length < length)
            {
                message = default;
                return false;
            }
            
            if (!reader.TryReadBigEndian(out int clientRevision))
            {
                message = default;
                return false;
            }
            
            if (!reader.TryReadBigEndian(out int clientRevisionPatch))
            {
                message = default;
                return false;
            }

            if (!reader.TryRead(out string serverToken))
            {
                message = default;
                return false;
            }
            
            consumed = reader.Position;
            examined = consumed;
            message = new HandshakeRequest(clientRevision, clientRevisionPatch, serverToken);
            return true;
        }

        public void WriteMessage(HandshakeResponse message, IBufferWriter<byte> output)
        {
            if (!message.Successful || message.UpdateKeys == null)
            {
                Span<byte> headerSpan = stackalloc byte[1];
                headerSpan[0] = message.Opcode;
                output.Write(headerSpan);
                return;
            }
            var buffer = MemoryBufferWriter.Get();
            try
            {
                buffer.WriteByte(message.Opcode);
                foreach (var key in message.UpdateKeys)
                {
                    buffer.WriteInt32BigEndian(key);
                }
                buffer.CopyTo(output);
            }
            finally
            {
                MemoryBufferWriter.Return(buffer);
            }
        }
    }
}