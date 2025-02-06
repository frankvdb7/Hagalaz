using System.Buffers;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentSpecialClickMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadLittleEndianA(out short extraData1))
            {
                message = null;
                return false;
            }
            if (!reader.TryReadMiddleEndian(out int clickData))
            {
                message = null;
                return false;
            }
            message = new InterfaceComponentClickMessage
            {
                ClickType = ComponentClickType.SpecialClick,
                InterfaceId = clickData >> 16,
                ChildId = clickData & 0xFFFF,
                ExtraData1 = extraData1,
                ExtraData2 = -1
            };
            return true;
        }
    }
}
