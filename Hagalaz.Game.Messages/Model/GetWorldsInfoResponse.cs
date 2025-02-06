using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class GetWorldsInfoResponse
    {
        public const byte Opcode = 5;
        
        public record Country
        {
            public string Name { get; set; } = default!;
            public int Flag { get; set; }
        }
        
        public record WorldInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
            public string? IpAddress { get; set; }
            public string CountryName { get; set; } = default!;
            public bool IsMembersOnly { get; set; }
            public bool IsQuickChatEnabled { get; set; }
            public bool IsPvP { get; set; }
            public bool IsLootShareEnabled { get; set; }
            public bool ShouldHighlight { get; set; }
            public int CharacterCount { get; set; }
        }
        
        public uint CharacterId { get; set; }
        public bool ContainsInformation { get; set; }
        public List<WorldInfo> Worlds { get; set; } = default!;
        public List<Country> Countries { get; set; } = default!;
    }
}