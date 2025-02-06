using System.Buffers;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class SetChatFilterMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out byte publicFilter) || !reader.TryRead(out byte privateFilter) || !reader.TryRead(out byte tradeFilter))
            {
                message = default;
                return false;
            }
            message = new SetChatFilterMessage
            {
                PublicFilter = (Availability)publicFilter,
                PrivateFilter = (Availability)privateFilter,
                TradeFilter = (Availability)tradeFilter,
            };
            return true;
        }
    }
}
