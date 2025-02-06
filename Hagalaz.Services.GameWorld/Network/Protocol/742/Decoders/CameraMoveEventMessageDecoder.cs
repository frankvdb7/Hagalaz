using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class CameraMoveEventMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndianA(out short x) || !reader.TryReadLittleEndian(out short y))
            {
                message = default;
                return false;
            }
            message = new CameraMoveEventMessage
            {
                X = x,
                Y = y,
            };
            return true;
        }
    }
}
