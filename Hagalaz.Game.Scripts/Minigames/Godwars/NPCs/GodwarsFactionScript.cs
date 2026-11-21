using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs
{
    /// <summary>
    ///     A member of a Godwars faction. It never aggros its own faction or familiars, and only targets creatures
    ///     within attack range plus one that fewer than three creatures recently attacked.
    /// </summary>
    /// <typeparam name="TFaction">The faction's script type: any NPC running a script of this type is a fellow member.</typeparam>
    public abstract class GodwarsFactionScript<TFaction>(
        INpc owner,
        INpcService npcService,
        ISimplePathFinder pathFinder,
        IWidgetScriptActivator widgetScriptActivator) : NpcScriptBase(owner, npcService, pathFinder, widgetScriptActivator)
        where TFaction : GodwarsFactionScript<TFaction>
    {
        /// <inheritdoc />
        public override bool IsAggressiveTowards(ICreature creature)
        {
            if (!Owner.WithinRange(creature, 1 + GetAttackDistance()))
            {
                return false;
            }

            if (creature is INpc { Script: TFaction or FamiliarScriptBase })
            {
                return false;
            }

            return creature.Combat.RecentAttackers.Count() <= 2;
        }

        /// <inheritdoc />
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

            var target = creatures.FirstOrDefault(c => IsAggressiveTowards(c) && Owner.Combat.CanSetTarget(c.Handle));
            if (target != null)
            {
                Owner.Combat.SetTarget(target.Handle);
            }
        }
    }
}
