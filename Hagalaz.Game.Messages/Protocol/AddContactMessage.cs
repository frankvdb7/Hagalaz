using System.Buffers;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class AddContactMessage : RaidoMessage
    {
        public required string ContactDisplayName { get; init; }
        public required short MessageLength { get; init; }
        public required ReadOnlySequence<byte> MessagePayload { get; set; }
    }
}