using System;
using System.Buffers;
using Hagalaz.Cache.Extensions;
using Hagalaz.Services.GameUpdate.Network.Messages;
using Microsoft.Extensions.Logging;
using Raido.Common.Buffers;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameUpdate.Network.Protocol
{
    public class FileProtocol : IRaidoProtocol
    {
        private readonly ILogger<FileProtocol> _logger;
        public string Name => "FileProtocol";
        public int Version => 742;
        public bool IsVersionSupported(int version) => version == Version;

        public FileProtocol(ILogger<FileProtocol> logger)
        {
            _logger = logger;
        }

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out byte opcode))
            {
                message = default;
                return false;
            }

            switch (opcode)
            {
                case 0: // non-priority
                case 1: // priority
                    if (!reader.TryRead(out byte cacheId))
                    {
                        message = default;
                        return false;
                    }

                    if (!reader.TryReadBigEndian(out int fileId))
                    {
                        message = default;
                        return false;
                    }

                    message = new PriorityFileRequest()
                    {
                        IndexId = cacheId, FileId = fileId, HighPriority = opcode == 1
                    };
                    break;
                case 2: // logged on
                case 3: // logged off
                    reader.Advance(5); // skip bytes
                    message = new AuthStatusChangedMessage()
                    {
                        Authenticated = opcode == 2
                    };
                    break;
                case 4:
                    if (!reader.TryRead(out byte encryptionFlag))
                    {
                        message = default;
                        return false;
                    }

                    reader.Advance(4); // skip bytes
                    message = new EncryptionFlagMessage()
                    {
                        EncryptionFlag = encryptionFlag
                    };
                    break;
                case 6: // connection initiated
                    reader.Advance(5); // skip bytes
                    message = default;
                    break;
                case 7: // connection done
                    message = default;
                    reader.Advance(5);
                    return false;
                default: // unknown opcode
                    message = default;
                    _logger.LogWarning($"Unknown opcode '{opcode}'");
                    return false;
            }

            consumed = reader.Position;
            examined = consumed;
            return true;
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
        {
            var buffer = new ArrayBufferWriter<byte>();
            WriteMessageBytes(message, buffer);
            return buffer.WrittenMemory;
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output) => WriteMessageBytes(message, output);

        public void WriteMessageBytes(RaidoMessage message, IBufferWriter<byte> output)
        {
            switch (message)
            {
                case PingMessage:
                    // not supported by client
                    return;
                case PriorityFileResponse response:
                    WriteFileBytes(response, output);
                    return;
                default: throw new NotImplementedException(nameof(message));
            }
        }

        private void WriteFileBytes(PriorityFileResponse response, IBufferWriter<byte> output)
        {
            var memoryBuffer = MemoryBufferWriter.Get();
            IBufferWriterProxy buffer = response.EncryptionFlag == 0
                ? new BufferWriterProxy(memoryBuffer)
                : new EncryptionBufferWriterProxy(memoryBuffer, response.EncryptionFlag);
            try
            {
                /* read the header of the container */
                var compression = response.Data.ReadSignedByte() & 0xFF;
                var compressedLength = ((response.Data.ReadSignedByte() & 0xFF) << 24) + ((response.Data.ReadSignedByte() & 0xFF) << 16) +
                                       ((response.Data.ReadSignedByte() & 0xFF) << 8) + ((response.Data.ReadSignedByte() & 0xFF));

                /* write container information */
                var attributes = compression;
                if (!response.HighPriority)
                {
                    attributes |= 0x80;
                }

                buffer.WriteByte(response.IndexId);
                buffer.WriteInt32(response.FileId);
                buffer.WriteByte((byte)attributes);
                buffer.WriteInt32(compressedLength);

                /* calculate the actual length and write the container data */
                int length = compression != 0 ? compressedLength + 4 : compressedLength;
                int blockOffset = 10; // 742 uses 10 bytes offset now
                for (int offset = 0; offset < length; offset++)
                {
                    if (blockOffset == 512)
                    {
                        buffer.WriteByte(byte.MaxValue);
                        blockOffset = 1;
                    }

                    buffer.WriteByte((byte)(response.Data.ReadSignedByte() & 0xFF)); // unsigned byte
                    blockOffset++;
                }

                memoryBuffer.CopyTo(output);
            }
            finally
            {
                MemoryBufferWriter.Return(memoryBuffer);
            }
        }
    }
}