using System.Buffers;
using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class NotifyFriendsChatMessage 
    {
        public const byte Opcode = 18;

        public class Claim
        {
            public string Name { get; set; } = default!;
        }
        
        public string ChatName { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? PreviousDisplayName { get; set; }
        public IReadOnlyCollection<Claim> Claims { get; set; } = default!;
        public int MessageLength { get; set; }
        public ReadOnlySequence<byte> MessagePayload { get; set; }
    }
}