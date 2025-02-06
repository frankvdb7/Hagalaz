using System.Buffers;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public abstract class InterfaceComponentClickMessageDecoderBase : IRaidoMessageDecoder
    {
        private readonly ComponentClickType _clickType;

        public InterfaceComponentClickMessageDecoderBase(ComponentClickType clickType) => _clickType = clickType;

        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadMiddleEndian(out int clickData))
            {
                message = null;
                return false;
            }
            if (!reader.TryReadLittleEndianA(out short extraData1))
            {
                message = null;
                return false;
            }
            if (!reader.TryReadLittleEndian(out short extraData2))
            {
                message = null;
                return false;
            }
            message = new InterfaceComponentClickMessage
            {
                ClickType = _clickType,
                InterfaceId = clickData >> 16,
                ChildId = clickData & 0xFFFF,
                ExtraData1 = extraData1,
                ExtraData2 = extraData2
            };
            return true;
        }
    }

    public class InterfaceComponentLeftClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentLeftClickDecoder() : base(ComponentClickType.LeftClick)
        {
        }
    }

    public class InterfaceComponentOption2ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption2ClickDecoder() : base(ComponentClickType.Option2Click)
        {
        }
    }

    public class InterfaceComponentOption3ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption3ClickDecoder() : base(ComponentClickType.Option3Click)
        {
        }
    }

    public class InterfaceComponentOption4ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption4ClickDecoder() : base(ComponentClickType.Option4Click)
        {
        }
    }

    public class InterfaceComponentOption5ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption5ClickDecoder() : base(ComponentClickType.Option5Click)
        {
        }
    }

    public class InterfaceComponentOption6ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption6ClickDecoder() : base(ComponentClickType.Option6Click)
        {
        }
    }

    public class InterfaceComponentOption7ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption7ClickDecoder() : base(ComponentClickType.Option7Click)
        {
        }
    }

    public class InterfaceComponentOption8ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption8ClickDecoder() : base(ComponentClickType.Option8Click)
        {
        }
    }

    public class InterfaceComponentOption9ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption9ClickDecoder() : base(ComponentClickType.Option9Click)
        {
        }
    }

    public class InterfaceComponentOption10ClickDecoder : InterfaceComponentClickMessageDecoderBase
    {
        public InterfaceComponentOption10ClickDecoder() : base(ComponentClickType.Option10Click)
        {
        }
    }
}
