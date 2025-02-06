using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class MouseMovementMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out byte messageOffset))
            {
                message = null;
                return false;
            }
            if (!reader.TryRead(out byte offsetX) || !reader.TryRead(out byte offsetY))
            {
                message = null;
                return false;
            }
            message = new MouseMovementMessage
            {

            };
            return true;
        }
    }
}
