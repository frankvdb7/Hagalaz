using System.Buffers;
using System.Collections.Generic;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class KeyboardEventMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndian(out short keyPressedCount))
            {
                message = default;
                return false;
            }
            var keys = new List<KeyboardEventMessage.KeyInfo>(keyPressedCount);
            for (var i = 0; i < keys.Count; i++)
            {
                if (!reader.TryRead(out byte eventCode))
                {
                    message = default;
                    return false;
                }
                if (!reader.TryReadInt24BigEndian(out int deltaTime))
                {
                    message = default;
                    return false;
                }
                keys.Add(new KeyboardEventMessage.KeyInfo
                {
                    EventCode = eventCode,
                    DeltaTime = deltaTime
                });
            }
            message = new KeyboardEventMessage
            {
                Keys = keys
            };
            return true;
        }
    }
}
