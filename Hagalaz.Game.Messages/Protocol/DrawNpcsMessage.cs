using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawNpcsMessage : RaidoMessage
    {
        public required ICharacter Character { get; init; } = default!;
        public required bool IsLargeSceneView { get; init; }
        public required LinkedList<INpc> LocalNpcs { get; init; } = default!;
        public required IReadOnlyList<INpc> VisibleNpcs { get; init; } = default!;
    }
}
