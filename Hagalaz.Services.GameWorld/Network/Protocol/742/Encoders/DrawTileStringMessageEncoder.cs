using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawTileStringMessageEncoder : IRaidoMessageEncoder<DrawTileStringMessage>
    {
        public void EncodeMessage(DrawTileStringMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(46)
            .SetSize(RaidoMessageSize.VariableByte)
            .WriteByte(0) // not used
            .WriteByte((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7))
            .WriteInt16BigEndian((short)message.Duration)
            .WriteByte((byte)message.Height)
            .WriteInt24BigEndian(message.Color)
            .WriteString(message.Value);
    }
}
