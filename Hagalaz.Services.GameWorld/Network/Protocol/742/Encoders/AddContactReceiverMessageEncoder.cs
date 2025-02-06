using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class AddContactReceiverMessageEncoder : IRaidoMessageEncoder<AddContactReceiverMessage>
    {
        public void EncodeMessage(AddContactReceiverMessage message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(136)
                .SetSize(RaidoMessageSize.VariableShort)
                .WriteByte(!string.IsNullOrEmpty(message.SenderPreviousDisplayName) ? (byte)1 : (byte)0)
                .WriteString(message.SenderDisplayName);
            if (!string.IsNullOrEmpty(message.SenderPreviousDisplayName))
            {
                output.WriteString(message.SenderPreviousDisplayName);
            }

            output.WriteInt16BigEndian((short)(message.Id >> 32))
                .WriteInt24BigEndian((int)(message.Id - ((message.Id >> 32) << 32)))
                .WriteByte((byte)message.SenderRights)
                .WriteInt16BigEndianSmart((short)message.MessageLength)
                .Write(message.MessagePayload);
        }
    }
}