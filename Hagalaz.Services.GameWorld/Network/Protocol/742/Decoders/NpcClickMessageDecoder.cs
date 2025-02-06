using System.Buffers;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public abstract class NpcClickMessageDecoderBase : IRaidoMessageDecoder
    {
        private readonly NpcClickType _clickType;

        public NpcClickMessageDecoderBase(NpcClickType clickType) => _clickType = clickType;

        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndian(out short index))
            {
                message = default;
                return false;
            }
            if (!reader.TryRead(out bool forceRun))
            {
                message = default;
                return false;
            }
            message = new NpcClickMessage
            {
                Index = index,
                ForceRun = forceRun,
                ClickType = _clickType
            };
            return true;
        }
    }

    public class NpcClickOption1MessageDecoder : NpcClickMessageDecoderBase
    {
        public NpcClickOption1MessageDecoder() : base(NpcClickType.Option1Click)
        {
        }
    }

    public class NpcClickOption2MessageDecoder : NpcClickMessageDecoderBase
    {
        public NpcClickOption2MessageDecoder() : base(NpcClickType.Option2Click)
        {
        }
    }

    public class NpcClickOption3MessageDecoder : NpcClickMessageDecoderBase
    {
        public NpcClickOption3MessageDecoder() : base(NpcClickType.Option3Click)
        {
        }
    }

    public class NpcClickOption4MessageDecoder : NpcClickMessageDecoderBase
    {
        public NpcClickOption4MessageDecoder() : base(NpcClickType.Option4Click)
        {
        }
    }

    public class NpcClickOption5MessageDecoder : NpcClickMessageDecoderBase
    {
        public NpcClickOption5MessageDecoder() : base(NpcClickType.Option5Click)
        {
        }
    }

    public class NpcClickOption6MessageDecoder : NpcClickMessageDecoderBase
    {
        public NpcClickOption6MessageDecoder() : base(NpcClickType.Option6Click)
        {
        }
    }
}
