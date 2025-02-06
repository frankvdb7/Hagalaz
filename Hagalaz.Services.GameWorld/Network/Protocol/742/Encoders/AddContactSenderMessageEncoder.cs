using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class AddContactSenderMessageEncoder : IRaidoMessageEncoder<AddContactSenderMessage>
    {
        public void EncodeMessage(AddContactSenderMessage message, IRaidoMessageBinaryWriter output) =>
            output.SetOpcode(137)
                .SetSize(RaidoMessageSize.VariableShort)
                .WriteString(message.ContactDisplayName)
                .WriteInt16BigEndianSmart((short)message.MessageLength)
                .Write(message.MessagePayload.FirstSpan);
    }
}