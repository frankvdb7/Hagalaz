using System.Buffers;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class AddContactSenderMessage : RaidoMessage
    {
        public required string ContactDisplayName { get; init; }
        public required int MessageLength { get; init; }
        public required ReadOnlySequence<byte> MessagePayload { get; init; }
    }
}