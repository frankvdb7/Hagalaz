using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    /// <summary>
    /// </summary>
    public abstract class BarrowBrother : NpcScriptBase
    {
        /// <summary>
        ///     The character target.
        /// </summary>
        protected ICharacter? CharacterTarget;

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
            if (creature == CharacterTarget)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Performs the aggressiveness check.
        /// </summary>
        public override void AggressivenessTick()
        {
            if (CharacterTarget == null || CharacterTarget.IsDestroyed)
            {
                return;
            }

            if (Owner.Combat.Target == null)
            {
                Owner.Combat.SetTarget(CharacterTarget);
            }
        }

        /// <summary>
        ///     Get's if this npc can be attacked by the specified character ('attacker').
        ///     By default , this method does check if this npc is attackable.
        ///     This method also checks if the attacker is a character, wether or not it
        ///     has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            if (attacker == CharacterTarget)
            {
                return true;
            }

            if (attacker is ICharacter)
            {
                ((ICharacter)attacker).SendChatMessage("This is not your target.");
            }

            return false;
        }

        /// <summary>
        ///     Determines whether this instance can attack the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override bool CanAttack(ICreature target)
        {
            if (Owner.Combat.Target == null || target == CharacterTarget)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Determines whether this instance can respawn.
        /// </summary>
        /// <returns></returns>
        public override bool CanRespawn() => false;

        /// <summary>
        ///     Determines whether this instance [can retaliate automatic] the specified creature.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns></returns>
        public override bool CanRetaliateTo(ICreature creature)
        {
            if (creature == CharacterTarget)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Called when [set target].
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnSetTarget(ICreature target) => CharacterTarget = target as ICharacter;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}