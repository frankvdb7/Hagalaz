using Raido.Common.Buffers;
using Raido.Common.Messages;

namespace Raido.Common.Protocol
{
    public interface IRaidoMessageEncoder
    {
        public void EncodeMessage(RaidoMessage message, IRaidoMessageBinaryWriter output);
    }

    public interface IRaidoMessageEncoder<in TMessage> : IRaidoMessageEncoder where TMessage : RaidoMessage
    {
        void IRaidoMessageEncoder.EncodeMessage(RaidoMessage raidoMessage, IRaidoMessageBinaryWriter output)
        {
            if (raidoMessage is TMessage message)
            {
                EncodeMessage(message, output);
            }
        }
        
        public void EncodeMessage(TMessage message, IRaidoMessageBinaryWriter output);
    }
}