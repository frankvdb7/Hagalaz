using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetCharacterOptionMessage : RaidoMessage
    {
        public required string Name { get; init; } = default!;
        public required CharacterClickType Type { get; init; }
        public required int IconId { get; init; }
        public required bool DrawOnTop { get; init; }
    }
}
