using Raido.Common.Buffers;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameUpdate.Network
{
    public class BufferWriterProxy : IBufferWriterProxy
    {
        private readonly MemoryBufferWriter _buffer;

        public BufferWriterProxy(MemoryBufferWriter buffer) => _buffer = buffer;

        public void WriteByte(byte value) => _buffer.WriteByte(value);

        public void WriteInt32(int value) => _buffer.WriteInt32BigEndian(value);
    }
}