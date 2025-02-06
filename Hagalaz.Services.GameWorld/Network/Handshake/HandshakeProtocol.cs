using System;
using System.Buffers;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Microsoft.Extensions.Options;
using Raido.Common.Buffers;
using Raido.Common.Messages;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake
{
    public class HandshakeProtocol : IRaidoProtocol
    {
        private readonly IRaidoCodec<HandshakeProtocol> _codec;
        private readonly ServerConfig _serverConfig;

        public string Name => "HandshakeProtocol";

        public int Version => _serverConfig.ClientRevision;

        public HandshakeProtocol(IRaidoCodec<HandshakeProtocol> codec, IOptions<ServerConfig> serverConfig)
        {
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _serverConfig = serverConfig.Value;
        }

        public bool IsVersionSupported(int version) => version == Version;

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage? message)
        {
            if (input.IsEmpty)
            {
                message = null;
                return false;
            }
            var data = input.FirstSpan;
            consumed = input.End;
            examined = consumed;
            var opcode = data[0];
            var payload = input.Slice(1);
            return _codec.TryDecodeMessage(opcode, in payload, out message);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
        {
            if (message is PingMessage)
            {
                return ReadOnlyMemory<byte>.Empty;
            }
            var buffer = MemoryBufferWriter.Get();
            try
            {
                if (!TryWriteMessageToBuffer(message, buffer))
                {
                    return ReadOnlyMemory<byte>.Empty;
                }
                return buffer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(buffer);
            }
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
        {
            if (message is PingMessage)
            {
                return;
            }
            var buffer = MemoryBufferWriter.Get();
            try
            {
                if (!TryWriteMessageToBuffer(message, output))
                {
                    return;
                }
                buffer.CopyTo(output);
            }
            finally
            {
                MemoryBufferWriter.Return(buffer);
            }
        }

        private bool TryWriteMessageToBuffer(RaidoMessage message, IBufferWriter<byte> output)
        {
            var buffer = MemoryBufferWriter.Get();
            var writer = new RaidoMessageBinaryWriter(buffer);
            var headerBuffer = ArrayPool<byte>.Shared.Rent(2);
            var headerPosition = 0;
            try
            {
                if (!_codec.TryEncodeMessage(message, writer))
                {
                    return false;
                }
                headerBuffer[headerPosition++] = (byte)writer.Opcode;
                if (buffer.Length > 0)
                {
                    headerBuffer[headerPosition++] = (byte)buffer.Length;
                }
                output.Write(headerBuffer.AsSpan()[..headerPosition]);
                buffer.CopyTo(output);
                return true;
            } 
            finally
            {
                MemoryBufferWriter.Return(buffer);
                ArrayPool<byte>.Shared.Return(headerBuffer);
            }
        }
    }
}
