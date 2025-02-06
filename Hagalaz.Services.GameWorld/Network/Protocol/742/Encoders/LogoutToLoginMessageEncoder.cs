using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class LogoutToLoginMessageEncoder : IRaidoMessageEncoder<LogoutToLoginMessage>
    {
        public void EncodeMessage(LogoutToLoginMessage message, IRaidoMessageBinaryWriter output) => output.SetSize(RaidoMessageSize.Fixed).SetOpcode(21);
    }
}
