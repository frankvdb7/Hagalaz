using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetCS2StringMessageEncoder : IRaidoMessageEncoder<SetCS2StringMessage>
    {
        public void EncodeMessage(SetCS2StringMessage message, IRaidoMessageBinaryWriter output)
        {

            if (message.Value.Length <= sbyte.MaxValue && message.Value.Length >= sbyte.MinValue)
            {
                output
                    .SetOpcode(50)
                    .SetSize(RaidoMessageSize.VariableByte)
                    .WriteInt16LittleEndian((short)message.Id)
                    .WriteString(message.Value);
            }
            else
            {
                output
                    .SetOpcode(55)
                    .SetSize(RaidoMessageSize.VariableShort)
                    .WriteInt16LittleEndianA((short)message.Id)
                    .WriteString(message.Value);
            }
        }
    }
}
