using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class ClientInfoMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadS(out byte framesPerSecond))
            {
                message = default;
                return false;
            }

            if (!reader.TryReadS(out byte garbageCollectionTime))
            {
                message = default;
                return false;
            }

            if (!reader.TryReadLittleEndianA(out short ping))
            {
                message = default;
                return false;
            }
            message = new ClientInfoMessage()
            {
                FramesPerSecond = framesPerSecond,
                GarbageCollectionTime = garbageCollectionTime,
                Ping = ping
            };
            return true;
        }
    }
}