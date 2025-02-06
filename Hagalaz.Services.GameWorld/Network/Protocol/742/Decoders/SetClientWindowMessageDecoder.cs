using System.Buffers;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class SetClientWindowMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out byte displayMode))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out short sizeX))
            {
                message = default; 
                return false;
            }
            if (!reader.TryReadBigEndian(out short sizeY)) 
            {
                message = default;
                return false; 
            }
            if (!reader.TryRead(out byte unknownSetting))
            {
                message = default;
                return false;
            }
            message = new SetClientWindowMessage
            {
                SizeX = sizeX,
                SizeY = sizeY,
                Mode = (DisplayMode)displayMode
            };
            return true;
        }
    }
}
