using System.Buffers;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Decoders
{
    public class ClientUpdateRequestDecoder : IRaidoMessageDecoder
    {
        public bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = ClientUpdateRequest.Instance;
            return true;
        }
    }
}
