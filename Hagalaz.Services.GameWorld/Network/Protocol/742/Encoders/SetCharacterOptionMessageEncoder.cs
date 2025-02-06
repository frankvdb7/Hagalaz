using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetCharacterOptionMessageEncoder : IRaidoMessageEncoder<SetCharacterOptionMessage>
    {
        public void EncodeMessage(SetCharacterOptionMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(151)
            .SetSize(RaidoMessageSize.VariableByte)
            .WriteString(message.Name)
            .WriteByteS((byte)message.Type)
            .WriteInt16BigEndianA((short)message.IconId)
            .WriteByteA((byte)(message.DrawOnTop ? 1 : 0));
    }
}
