using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    /// <summary>
    /// </summary>
    public class SergeantStrongstack : BodyGuard
    {
        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Crush;

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [6261];

        /// <summary>
        ///     Get's if this npc can be poisoned.
        ///     By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can poison; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanPoison() => false;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}