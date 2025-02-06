using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class FriendsListMessageEncoder : IRaidoMessageEncoder<FriendsListMessage>
    {
        public void EncodeMessage(FriendsListMessage message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(140).SetSize(RaidoMessageSize.VariableShort);
            foreach (var friend in message.Friends)
            {
                var worldId = friend.WorldId ?? 0;
                output
                    .WriteByte((byte)(message.Notify ? 0 : 1))
                    .WriteString(friend.DisplayName)
                    .WriteString(friend.PreviousDisplayName ?? string.Empty)
                    .WriteInt16BigEndian((short)worldId)
                    .WriteByte((byte)(friend.Rank ?? 0))
                    .WriteByte(0); // unknown bool

                if (worldId > 0)
                {
                    output.WriteString(friend.WorldName ?? string.Empty)
                        .WriteByte(0)
                        .WriteInt32BigEndian(0); // something with item id on map
                }
            }
        }
    }
}
