using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class NpcClickMessage : RaidoMessage
    {
        public required int Index { get; init; }
        public required bool ForceRun { get; init; }
        public required NpcClickType ClickType { get; init; }
    }
}
