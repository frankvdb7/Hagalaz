using System.Buffers;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders
{
    public class InterfaceComponentRemovedMessageDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = new InterfaceComponentRemovedMessage();
            return true;
        }
    }
}
