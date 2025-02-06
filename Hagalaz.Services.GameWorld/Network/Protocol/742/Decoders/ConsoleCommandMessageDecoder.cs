using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class ConsoleCommandMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryRead(out bool isClientGeneratedCommand) || !reader.TryRead(out bool isTabPressed))
            {
                message = default;
                return false;
            }
            if (!reader.TryRead(out string command))
            {
                message = default;
                return false;
            }

            message = new ConsoleCommandMessage
            {
                Command = command
            };
            return true;
        }
    }
}
