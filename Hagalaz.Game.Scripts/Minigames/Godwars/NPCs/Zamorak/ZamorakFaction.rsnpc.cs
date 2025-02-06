using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zamorak
{
    /// <summary>
    ///     Contains the zamorak faction npc script.
    /// </summary>
    public class ZamorakFaction : NpcScriptBase
    {
        /// <summary>
        ///     Get's if this npc can aggro attack specific character.
        ///     By default this method does check if character is character.
        ///     This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsAggressiveTowards(ICreature creature)
        {
            if (!Owner.WithinRange(creature, 1 + GetAttackDistance()))
            {
                return false;
            }

            if (creature is INpc)
            {
                var script = ((INpc)creature).Script;
                if (script is ZamorakFaction || script is FamiliarScriptBase)
                {
                    return false;
                }
            }

            if (creature.Combat.RecentAttackers.Count() > 2)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Performs the aggressiveness check.
        /// </summary>
        public override void AggressivenessTick()
        {
            if (Owner.Combat.IsInCombat())
            {
                return;
            }

            var creatures = Owner.Viewport.VisibleCreatures;
            if (!creatures.OfType<ICharacter>().Any())
            {
                return;
            }

            var c = creatures.Where(c => IsAggressiveTowards(c) && Owner.Combat.CanSetTarget(c)).FirstOrDefault();
            if (c != null)
            {
                Owner.Combat.SetTarget(c);
            }
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => GodwarsConstants.ZamorakFactionNpCs;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}