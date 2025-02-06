using System.Buffers;

namespace Raido.Common.Protocol
{
    public interface IRaidoMessageWriter<in TMessage>
    {
        void WriteMessage(TMessage message, IBufferWriter<byte> output);
    }
}