using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentDragMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndian(out short fromExtraData2))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndianA(out short fromExtraData1)) 
            {
                message = default;
                return false;
            }   
            if (!reader.TryReadBigEndianA(out short toExtraData1))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out short toExtraData2))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out int fromHash))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out int toHash))
            {
                message = default;
                return false;
            }
            var fromInterfaceID = fromHash >> 16;
            var fromComponentID = fromHash & 0xFFFF;
            var toInterfaceID = toHash >> 16;
            var toComponentID = toHash & 0xFFFF;
            message = new InterfaceComponentDragMessage
            {
                FromId = fromInterfaceID,
                FromComponentId= fromComponentID,
                FromExtraData1 = fromExtraData1,
                FromExtraData2 = fromExtraData2,
                ToId = toInterfaceID,
                ToComponentId = toComponentID,
                ToExtraData1 = toExtraData1,
                ToExtraData2 = toExtraData2,
            };
            return true;
        }
    }
}
