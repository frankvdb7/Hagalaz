using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Encoders
{
    public class WorldSignInResponseEncoder : IRaidoMessageEncoder<WorldSignInResponse>
    {
        public void EncodeMessage(WorldSignInResponse message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(ClientSignInResponse.Success.GetOpcode())
                .SetSize(RaidoMessageSize.VariableByte);

            output.WriteByte((byte)message.ClientPermissions);
            output.WriteByte(0); // does something between 5 >= and <= 9 (client logging level?)

            output.WriteByte(0); // has something to do with chat muted???
            output.WriteByte(0); // something to do with chat muted???

            output.WriteByte(0); // executes some kind of 'zap' on the client frame

            output.WriteByte(message.IsQuickChatOnly ? (byte)1 : (byte)0);

            output.WriteInt16BigEndian((short)message.CharacterWorldIndex);

            output.WriteByte(1); // character is member

            output.WriteInt24BigEndian(0); // 343932928, has something to do with female / male???

            output.WriteByte(message.IsMembersOnly ? (byte)1 : (byte)0);

            output.WriteString(message.DisplayName);
        }
    }
}