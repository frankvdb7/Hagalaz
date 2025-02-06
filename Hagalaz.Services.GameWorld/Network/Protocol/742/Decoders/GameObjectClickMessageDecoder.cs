using System.Buffers;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public abstract class GameObjectClickMessageDecoderBase : IRaidoMessageDecoder
    {
        private readonly GameObjectClickType _clickType;

        public GameObjectClickMessageDecoderBase(GameObjectClickType clickType) => _clickType = clickType;

        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadMixedEndian(out int id))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out short absY) || !reader.TryReadBigEndianA(out short absX))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadS(out bool forceRun))
            {
                message = default;
                return false;
            }
            message = new GameObjectClickMessage
            {
                Id = id,
                AbsX = absX,
                AbsY = absY,
                ForceRun = forceRun,
                ClickType = _clickType
            };
            return true;
        }
    }

    public class GameObjectClickOption1MessageDecoder : GameObjectClickMessageDecoderBase
    {
        public GameObjectClickOption1MessageDecoder() : base(GameObjectClickType.Option1Click) { }
    }

    public class GameObjectClickOption2MessageDecoder : GameObjectClickMessageDecoderBase
    {
        public GameObjectClickOption2MessageDecoder() : base(GameObjectClickType.Option2Click) { }
    }

    public class GameObjectClickOption3MessageDecoder : GameObjectClickMessageDecoderBase
    {
        public GameObjectClickOption3MessageDecoder() : base(GameObjectClickType.Option3Click) { }
    }

    public class GameObjectClickOption4MessageDecoder : GameObjectClickMessageDecoderBase
    {
        public GameObjectClickOption4MessageDecoder() : base(GameObjectClickType.Option4Click) { }
    }

    public class GameObjectClickOption5MessageDecoder : GameObjectClickMessageDecoderBase
    {
        public GameObjectClickOption5MessageDecoder() : base(GameObjectClickType.Option5Click) { }
    }

    public class GameObjectClickOption6MessageDecoder : GameObjectClickMessageDecoderBase
    {
        public GameObjectClickOption6MessageDecoder() : base(GameObjectClickType.Option6Click) { }
    }
}
