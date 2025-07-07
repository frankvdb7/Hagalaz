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
    [NpcScriptMetaData([6210, 6211, 6212, 6213, 6214, 6215, 6216, 6217, 6218, 6219, 6220, 6221])]
    public class ZamorakFaction : NpcScriptBase
    {
        /// <summary>
        ///     Get's if this npc can aggro attack specific character.
        ///     By default this method does check if character is character.
        ///     This method does not get called by ticks if npc reaction is not aggressive.
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

            if (creature is not INpc npc)
            {
                return creature.Combat.RecentAttackers.Count() <= 2;
            }

            var script = npc.Script;
            if (script is ZamorakFaction or FamiliarScriptBase)
            {
                return false;
            }

            return creature.Combat.RecentAttackers.Count() <= 2;
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

            var c = creatures.FirstOrDefault(c => IsAggressiveTowards(c) && Owner.Combat.CanSetTarget(c));
            if (c != null)
            {
                Owner.Combat.SetTarget(c);
            }
        }
    }
}