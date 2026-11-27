using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class AddIgnoreMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out string displayName))
            {
                message = default;
                return false;
            }
            if (!reader.TryRead(out bool ignoreTemporary))
            {
                message = default;
                return false;
            }
            message = new AddIgnoreMessage
            {
                DisplayName = displayName
            };
            return true;
        }
    }
}
