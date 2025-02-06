using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class NotifyFriendsChatMemberLeave
    {
        public const byte Opcode = 15;

        public class Member
        {
            public string DisplayName { get; set; } = default!;
            public string? PreviousDisplayName { get; set; }
            public int WorldId { get; set; }
        }
        
        public string ChatName { get; set; } = default!;
        public IReadOnlyCollection<Member> Members { get; set; } = default!;
    }
}