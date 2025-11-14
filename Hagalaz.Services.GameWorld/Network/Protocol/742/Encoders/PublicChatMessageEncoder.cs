using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class PublicChatMessageEncoder : IRaidoMessageEncoder<PublicChatMessage>
    {
        private readonly IHuffmanEncoder _huffmanEncoder;

        public PublicChatMessageEncoder(IHuffmanEncoder huffmanEncoder) => _huffmanEncoder = huffmanEncoder;

        public void EncodeMessage(PublicChatMessage message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(57)
                .SetSize(RaidoMessageSize.VariableByte)
                .WriteInt16BigEndian((short)message.CharacterIndex!.Value)
                .WriteByte((byte)message.TextColor)
                .WriteByte((byte)message.TextAnimation)
                .WriteByte((byte)message.Permissions!.Value.ToClientRights())
                .WriteInt16BigEndianSmart((short)message.Text.Length);
            
            _huffmanEncoder.Encode(message.Text, output);
        }
    }
}