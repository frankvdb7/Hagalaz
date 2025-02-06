using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class NotifyCharacterSignOut
    {
        public const byte Opcode = 13;

        public record Character(uint MasterId, string DisplayName, int? WorldId)
        {
            public string? PreviousDisplayName { get; init; }
        }
        
        public IReadOnlyCollection<Character> Characters { get; }

        public NotifyCharacterSignOut(IReadOnlyCollection<Character> characters) => Characters = characters;
    }
}