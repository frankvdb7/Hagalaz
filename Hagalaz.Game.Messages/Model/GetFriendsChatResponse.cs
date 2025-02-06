using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class GetFriendsChatResponse
    {
        public const byte Opcode = 16;

        public enum ResponseCode : byte
        {
            Success = 0,
            NotFound = 1,
        }
        
        public record Member
        {
            public string DisplayName { get; init; } = default!;
            public string? PreviousDisplayName { get; init; }
            public int WorldId { get; init; }
            public object Rank { get; init; }
        }
        
        public uint CharacterId { get; init; }
        public ResponseCode Response { get; init; }
        public string? ChatName { get; init; }
        public string? OwnerDisplayName { get; init; }
        public string? OwnerPreviousDisplayName { get; init; }
        public IReadOnlyCollection<Member>? Members { get; init; }
        public object? RankToKick { get; init; }
    }
}