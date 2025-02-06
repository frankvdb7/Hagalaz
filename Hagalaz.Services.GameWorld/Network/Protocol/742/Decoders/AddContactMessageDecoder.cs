using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class AddContactMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out string contactDisplayName))
            {
                message = default;
                return false;
            }

            if (!reader.TryReadBigEndianSmart(out short messageLength))
            {
                message = default;
                return false;
            }

            message = new AddContactMessage()
            {
                ContactDisplayName = contactDisplayName,
                MessageLength = messageLength,
                MessagePayload = reader.UnreadSequence
            };
            return true;
        }
    }
}