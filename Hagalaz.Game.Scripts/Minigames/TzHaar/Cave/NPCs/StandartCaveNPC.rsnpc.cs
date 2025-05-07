using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.NPCs
{
    [NpcScriptMetaData([2738])]
    public class StandardCaveNpc : NpcScriptBase
    {
        protected readonly INpcBuilder NpcBuilder;

        public StandardCaveNpc(INpcBuilder npcBuilder) => NpcBuilder = npcBuilder;

        /// <summary>
        ///     Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override bool CanSetTarget(ICreature target)
        {
            if (target.Location.Dimension != Owner.Location.Dimension)
            {
                return false;
            }

            if (!target.Area.Script.CanBeAttacked(target, Owner))
            {
                return false;
            }

            if (!Owner.Area.Script.CanAttack(Owner, target))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Get's if this npc can aggro attack specific character.
        ///     By default this method does check if creature is character.
        ///     This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsAggressiveTowards(ICreature creature) => creature is ICharacter;

        /// <summary>
        ///     Get's if this npc can attack the specified character ('target').
        ///     By default , this method returns true.
        /// </summary>
        /// <param name="target">Creature which is being attacked by this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanAttack(ICreature target) => true;

        /// <summary>
        ///     Determines whether this instance can respawn.
        ///     If false, then the npc will be unregistered from the world.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can spawn; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanRespawn() => false;

        /// <summary>
        ///     Performs the aggressiveness check.
        /// </summary>
        public override void AggressivenessTick()
        {
            if (Owner.Combat.IsInCombat())
            {
                return;
            }

            var character = Owner.Viewport.VisibleRegions.SelectMany(r => r.FindAllCharacters()).FirstOrDefault();
            if (character != null)
            {
                Owner.Combat.SetTarget(character);
            }
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}