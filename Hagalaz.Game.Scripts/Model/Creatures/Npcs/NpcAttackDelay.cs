using System;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    ///     Owns the distance-based hit delay of ranged and magic NPC attacks: a base delay plus five ticks per tile of
    ///     horizontal and vertical distance.
    /// </summary>
    internal static class NpcAttackDelay
    {
        public static int FromDistance(INpc owner, ICreature target, int baseDelay = 20) =>
            baseDelay + Math.Abs(owner.Location.X - target.Location.X) * 5 + Math.Abs(owner.Location.Y - target.Location.Y) * 5;
    }
}
