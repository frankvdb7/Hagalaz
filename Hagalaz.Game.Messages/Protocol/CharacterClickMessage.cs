using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class CharacterClickMessage : RaidoMessage
    {
        public required int Index { get; init; }
        public required bool ForceRun { get; init; }
        public required CharacterClickType ClickType { get; init; }
    }
}
