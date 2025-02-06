using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class MouseEventMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndianA(out short deltaTime))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadC(out byte eventData))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out int positionData))
            {
                message = default;
                return false;
            }
            message = new MouseEventMessage
            {
                EventCode = eventData >> 1,
                ScreenX = positionData >> 16,
                ScreenY = positionData & 0xFFFF,
                DeltaTime = deltaTime
            };
            return true;
        }
    }
}
