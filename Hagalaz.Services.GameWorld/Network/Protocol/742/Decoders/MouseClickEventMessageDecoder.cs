using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class MouseClickEventMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndianA(out short mouseData))
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
            message = new MouseClickEventMessage
            {
                LeftClick = mouseData >> 15 == 1,
                ScreenX = positionData >> 16,
                ScreenY = positionData & 0xFFFF,
                DeltaTime = mouseData & 0xFFFF
            };
            return true;
        }
    }
}
