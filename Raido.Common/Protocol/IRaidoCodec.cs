using System.Buffers;

namespace Raido.Common.Protocol
{
    public interface IRaidoCodec<TProtocol> where TProtocol : IRaidoProtocol
    {
        bool TryDecodeMessage(int opcode, in ReadOnlySequence<byte> input, out RaidoMessage? message);
        bool TryEncodeMessage<TMessage>(TMessage message, IRaidoMessageBinaryWriter output) where TMessage : RaidoMessage;
    }
}