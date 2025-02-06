using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class MusicPlayedMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndian(out int musicId))
            {
                message = default;
                return false;
            }
            message = new MusicPlayedMessage
            {
                MusicId = musicId,
            };
            return true;
        }
    }
}
