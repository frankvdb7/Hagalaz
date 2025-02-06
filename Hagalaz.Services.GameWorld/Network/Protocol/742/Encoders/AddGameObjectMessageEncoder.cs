using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class AddGameObjectMessageEncoder : IRaidoMessageEncoder<AddGameObjectMessage>
    {
        public void EncodeMessage(AddGameObjectMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(43)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32MixedEndian(message.GameObjectId)
            .WriteByteS((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7))
            .WriteByteA((byte)(message.Rotation & 0x3 | (byte)message.Shape << 2));
    }
}
