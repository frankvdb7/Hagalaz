using System.Buffers;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public abstract class GroundItemClickMessageDecoderBase : IRaidoMessageDecoder
    {
        private readonly GroundItemClickType _clickType;

        public GroundItemClickMessageDecoderBase(GroundItemClickType clickType) => _clickType = clickType;

        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out bool forceRun))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndianA(out short itemId))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadLittleEndian(out short absY) || !reader.TryReadBigEndianA(out short absX))
            {
                message = default;
                return false;
            }
            message = new GroundItemClickMessage()
            {
                Id = itemId,
                AbsX = absX,
                AbsY = absY,
                ForceRun = forceRun,
                ClickType = _clickType
            };
            return true;
        }
    }

    public class GroundItemClickOption1MessageDecoder : GroundItemClickMessageDecoderBase
    {
        public GroundItemClickOption1MessageDecoder() : base(GroundItemClickType.Option1Click) { }
    }

    public class GroundItemClickOption2MessageDecoder : GroundItemClickMessageDecoderBase
    {
        public GroundItemClickOption2MessageDecoder() : base(GroundItemClickType.Option2Click) { }
    }

    public class GroundItemClickOption3MessageDecoder : GroundItemClickMessageDecoderBase
    {
        public GroundItemClickOption3MessageDecoder() : base(GroundItemClickType.Option3Click) { }
    }

    public class GroundItemClickOption4MessageDecoder : GroundItemClickMessageDecoderBase
    {
        public GroundItemClickOption4MessageDecoder() : base(GroundItemClickType.Option4Click) { }
    }

    public class GroundItemClickOption5MessageDecoder : GroundItemClickMessageDecoderBase
    {
        public GroundItemClickOption5MessageDecoder() : base(GroundItemClickType.Option5Click) { }
    }

    public class GroundItemClickOption6MessageDecoder : GroundItemClickMessageDecoderBase
    {
        public GroundItemClickOption6MessageDecoder() : base(GroundItemClickType.Option6Click) { }
    }
}
