using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class MovementMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out bool forceRun))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndianA(out short absX))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out short absY))
            {
                message = default;
                return false;
            }
            message = new MovementMessage
            {
                ForceRun = forceRun,
                AbsX = absX,
                AbsY = absY
            };
            return true;
        }
    }
}
