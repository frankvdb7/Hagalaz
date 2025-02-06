using System;
using System.Collections.Generic;

namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record FriendsChatDto
    {
        public record Claim
        {
            public string Name { get; init; } = default!;
        }
        
        public record Member
        {
            public string DisplayName { get; init; } = default!;
            public string? PreviousDisplayName { get; init; }
            public FriendsChatRank Rank { get; init; }
            public int WorldId { get; init; }
            public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
        }

        public string ChatName { get; init; } = default!;
        public string OwnerDisplayName { get; init; } = default!;
        public string? OwnerPreviousDisplayName { get; init; }
        public IReadOnlyCollection<Member> Members { get; init; } = Array.Empty<Member>();
        public FriendsChatRank RankToKick { get; init; }
    }
}