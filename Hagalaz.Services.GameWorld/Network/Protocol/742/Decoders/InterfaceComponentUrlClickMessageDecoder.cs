using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentUrlClickMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndian(out short totalStringLength))
            {
                message = default;
                return false;
            }
            if (!reader.TryRead(out string type) || !reader.TryRead(out string title) || !reader.TryRead(out string unknown))
            {
                message = default;
                return false;
            }
            if (!reader.TryRead(out byte flag))
            {
                message = default;
                return false;
            }
            var someBool = (flag & 0x1) != 0;
            var someBool2 = (flag & 0x2) != 0;
            message = new InterfaceComponentUrlClickMessage
            {
                Type = type,
                Title = title,
                Unknown = unknown,
                SomeBool = someBool,
                SomeBool2 = someBool2,
                StringLength = totalStringLength
            };
            return true;
        }
    }
}
