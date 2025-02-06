namespace Hagalaz.Services.GameUpdate.Network
{
    public interface IBufferWriterProxy
    {
        void WriteByte(byte value);
        void WriteInt32(int value);
    }
}