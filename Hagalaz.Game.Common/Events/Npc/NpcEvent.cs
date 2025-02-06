using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Common.Events.Npc
{
    /// <summary>
    /// Abstract class for npc event.
    /// </summary>
    public abstract class NpcEvent : CreatureEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NpcEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        protected NpcEvent(INpc target)
            : base(target)
        {
        }
    }
}