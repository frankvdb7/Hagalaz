using System.Buffers;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class PublicChatMessageDecoder : IRaidoMessageDecoder
    {
        private readonly IHuffmanDecoder _huffmanDecoder;

        public PublicChatMessageDecoder(IHuffmanDecoder huffmanDecoder) => _huffmanDecoder = huffmanDecoder;

        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out byte textColor))
            {
                message = default;
                return false;
            }
            if (!reader.TryRead(out byte textAnimation))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndianSmart(out short textLength))
            {
                message = default;
                return false;
            }

            var textInput = reader.Sequence.Slice(reader.Position);
            if (!_huffmanDecoder.TryDecode(textInput, textLength, out var text))
            {
                message = default;
                return false;
            }
            message = new PublicChatMessage()
            {
                Text = text, TextAnimation = textAnimation, TextColor = textColor
            };
            return true;
        }
    }
}