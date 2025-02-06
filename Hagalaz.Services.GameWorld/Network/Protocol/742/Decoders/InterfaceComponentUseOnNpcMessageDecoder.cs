using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentUseOnNpcMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadBigEndianA(out short index))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndianA(out short extraData1))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out short extraData2))
            {
                message = default;
                return false;
            }
            if (!reader.TryRead(out bool forceRun))
            {
                message = default;
                return false;
            }
            if (!reader.TryReadBigEndian(out int interfaceHash))
            {
                message = default;
                return false;
            }
            message = new InterfaceComponentUseOnNpcMessage
            {
                InterfaceId = interfaceHash >> 16,
                ComponentId = interfaceHash & 0xFFFF,
                ExtraData1 = extraData1,
                ExtraData2 = extraData2,
                ForceRun = forceRun,
                Index = index
            };
            return true;
        }
    }
}
