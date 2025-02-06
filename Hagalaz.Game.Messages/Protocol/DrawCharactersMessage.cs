using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawCharactersMessage : RaidoMessage
    {
        public required ICharacter Character { get; init; } = default!;
        public required IDictionary<int, ICharacter> AllCharacters { get; init; } = default!;
        public required LinkedList<ICharacter> LocalCharacters { get; init; } = default!;
    }
}
