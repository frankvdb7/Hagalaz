using System.Buffers;
using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class NotifyContactMessage
    {
        public const byte Opcode = 20; 
        
        public record Claim
        {
            public string Name { get; init; } = default!;
        }

        public record SenderDto
        {
            public string DisplayName { get; init; } = default!;
            public string? PreviousDisplayName { get; init; }
            public IReadOnlyCollection<Claim>? Claims { get; init; }
        }
        public uint ContactId { get; init; }
        public SenderDto Sender { get; init; } = default!;
        public int MessageLength { get; init; }
        public ReadOnlySequence<byte> MessagePayload { get; init; }
    }
}