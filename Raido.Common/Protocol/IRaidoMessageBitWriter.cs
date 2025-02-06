namespace Raido.Common.Protocol
{
    public interface IRaidoMessageBitWriter : IRaidoMessageBitWriterEnd
    {
        IRaidoMessageBitWriter WriteBits(int bitCount, int value);
    }

    public interface IRaidoMessageBitWriterBegin
    {
        IRaidoMessageBitWriter BeginBitAccess();
    }

    public interface IRaidoMessageBitWriterEnd
    {
        IRaidoMessageBinaryWriter EndBitAccess();
    }
}
