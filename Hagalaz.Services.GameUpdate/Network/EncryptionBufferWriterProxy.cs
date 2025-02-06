using Raido.Common.Buffers;

namespace Hagalaz.Services.GameUpdate.Network
{
    public class EncryptionBufferWriterProxy : IBufferWriterProxy
    {
        private readonly MemoryBufferWriter _buffer;
        private readonly int _encryptionFlag;

        public EncryptionBufferWriterProxy(MemoryBufferWriter buffer, int encryptionFlag)
        {
            _buffer = buffer;
            _encryptionFlag = encryptionFlag;
        }

        public void WriteByte(byte value) => _buffer.WriteByte((byte)(value ^ _encryptionFlag));

        public void WriteInt32(int value)
        {
            _buffer.WriteByte((byte)((value >> 24) ^ _encryptionFlag));
            _buffer.WriteByte((byte)((value >> 16) ^ _encryptionFlag));
            _buffer.WriteByte((byte)((value >> 8) ^ _encryptionFlag));
            _buffer.WriteByte((byte)(value ^ _encryptionFlag));
        }
    }
}