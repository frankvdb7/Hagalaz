using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class LogoutToLobbyMessageEncoder : IRaidoMessageEncoder<LogoutToLobbyMessage>
    {
        public void EncodeMessage(LogoutToLobbyMessage message, IRaidoMessageBinaryWriter output) => output.SetSize(RaidoMessageSize.Fixed).SetOpcode(35);
    }
}
