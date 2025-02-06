using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawCharacterComponentMessageEncoder : IRaidoMessageEncoder<DrawCharacterComponentMessage>
    {
        public void EncodeMessage(DrawCharacterComponentMessage message, IRaidoMessageBinaryWriter output) 
        { 
            if (message.DrawSelf)
            {
                output
                    .SetOpcode(49)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt32MiddleEndian(message.ComponentId << 16 | message.ChildId);
            } 
            else
            {
                output
                    .SetOpcode(5)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt32BigEndian(message.CharacterName.Value)
                    .WriteInt32MixedEndian(message.ComponentId << 16 | message.ChildId)
                    .WriteInt16LittleEndianA((short)message.CharacterIndex.Value);
            }
         }
    }
}
