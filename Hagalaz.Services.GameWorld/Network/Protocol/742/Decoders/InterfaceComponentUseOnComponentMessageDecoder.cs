using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentUseOnComponentMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndianA(out short onExtraData2))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out int interfaceHash))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out short extraData1))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out short extraData2))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndianA(out short onExtraData1))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out int onHash))
            {
                message = default;
                return false;
            }
            var interfaceId = interfaceHash >> 16;
            var componentId = interfaceHash & 0xFFFF;
            var onInterfaceId = onHash >> 16;
            var onComponentId = onHash & 0xFFFF;
            message = new InterfaceComponentUseOnComponentMessage
            {
                InterfaceId = interfaceId,
                ComponentId = componentId,
                OnInterfaceId = onInterfaceId,
                OnComponentId = onComponentId,
                ExtraData1 = extraData1,
                ExtraData2 = extraData2,
                OnExtraData1 = onExtraData1,
                OnExtraData2 = onExtraData2,
            };
            return true;
        }
    }
}
