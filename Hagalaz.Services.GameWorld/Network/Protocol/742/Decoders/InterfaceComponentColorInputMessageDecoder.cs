using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentColorInputMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndian(out short value)) 
            {
                message = default;
                return false;
            }
            message = new InterfaceComponentColorInputMessage
            {
                Value = value
            };
            return true;
        }
    }
}
