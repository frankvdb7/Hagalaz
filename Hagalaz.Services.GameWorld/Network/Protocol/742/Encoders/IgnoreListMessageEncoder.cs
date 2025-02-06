using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class IgnoreListMessageEncoder : IRaidoMessageEncoder<IgnoreListMessage>
    {
        public void EncodeMessage(IgnoreListMessage message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(98).SetSize(RaidoMessageSize.VariableShort);
            foreach (var ignore in message.Ignores)
            {
                var flags = 0;
                if (!string.IsNullOrEmpty(ignore.PreviousDisplayName))
                {
                    flags |= 0x1;
                }
                // flags 0x2 == some bool
                output.WriteByte((byte)flags)
                    .WriteString(ignore.DisplayName)
                    .WriteString(ignore.PreviousDisplayName ?? string.Empty);
            }
        }
    }
}
