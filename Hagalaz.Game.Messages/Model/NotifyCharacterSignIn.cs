using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class NotifyCharacterSignIn
    {
        public const byte Opcode = 12;
        public record Character(uint Id, string DisplayName, int? WorldId)
        {
            public string? PreviousDisplayName { get; init; }
        }
        
        public IReadOnlyCollection<Character> Characters { get; }

        public NotifyCharacterSignIn(IReadOnlyCollection<Character> characters) => Characters = characters;
    }
}