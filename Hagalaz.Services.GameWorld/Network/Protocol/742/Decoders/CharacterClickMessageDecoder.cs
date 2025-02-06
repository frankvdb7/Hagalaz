using System.Buffers;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public abstract class CharacterClickMessageDecoderBase : IRaidoMessageDecoder
    {
        private readonly CharacterClickType _clickType;

        public CharacterClickMessageDecoderBase(CharacterClickType clickType) => _clickType = clickType;

        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out bool forceRun))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndianA(out short index))
            {
                message = default;
                return false;
            }
            message = new CharacterClickMessage
            {
                Index = index,
                ForceRun = forceRun,
                ClickType = _clickType,
            };
            return true;
        }
    }

    public class CharacterClickOption1Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption1Decoder() : base(CharacterClickType.Option1Click) { }
    }

    public class CharacterClickOption2Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption2Decoder() : base(CharacterClickType.Option2Click) { }
    }

    public class CharacterClickOption3Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption3Decoder() : base(CharacterClickType.Option3Click) { }
    }

    public class CharacterClickOption4Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption4Decoder() : base(CharacterClickType.Option4Click) { }
    }

    public class CharacterClickOption5Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption5Decoder() : base(CharacterClickType.Option5Click) { }
    }

    public class CharacterClickOption6Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption6Decoder() : base(CharacterClickType.Option6Click) { }
    }

    public class CharacterClickOption7Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption7Decoder() : base(CharacterClickType.Option7Click) { }
    }

    public class CharacterClickOption8Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption8Decoder() : base(CharacterClickType.Option8Click) { }
    }

    public class CharacterClickOption9Decoder : CharacterClickMessageDecoderBase
    {
        public CharacterClickOption9Decoder() : base(CharacterClickType.Option9Click) { }
    }
}
