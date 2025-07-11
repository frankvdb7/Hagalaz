﻿using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    /// <summary>
    ///     Contains the bandos faction npc script.
    /// </summary>
    [NpcScriptMetaData([6268, 6269, 6270, 6271, 6272, 6273, 6274, 6275, /*6276,*/ 6277, /*6278,*/ 6279, 6280, 6281, 6282, 6283, 9185, 374])]
    public class BandosFaction : NpcScriptBase
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

            if (creature is INpc npc)
            {
                var script = npc.Script;
                if (script is BandosFaction or FamiliarScriptBase)
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

            var c = creatures.FirstOrDefault(c => IsAggressiveTowards(c) && Owner.Combat.CanSetTarget(c));
            if (c != null)
            {
                Owner.Combat.SetTarget(c);
            }
        }
    }
}