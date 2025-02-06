using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    ///     Class for holding attacker information
    /// </summary>
    public class CreatureAttackerInfo : ICreatureAttackerInfo
    {
        /// <summary>
        ///     Construct's new attacker class.
        /// </summary>
        /// <param name="creature">Creature which performed attack.</param>
        /// <param name="lastAttackTick">Contains tick in which the attack was performed.</param>
        public CreatureAttackerInfo(ICreature creature, int lastAttackTick)
        {
            Attacker = creature;
            LastAttackTick = lastAttackTick;
        }

        /// <summary>
        ///     Gets or sets the damage.
        /// </summary>
        /// <value>The damage.</value>
        public int TotalDamage { get; set; }

        /// <summary>
        ///     Contains last tick
        /// </summary>
        /// <value>The last attack tick.</value>
        public int LastAttackTick { get; set; }

        /// <summary>
        ///     Contains attacker.
        /// </summary>
        /// <value>The attacker creature.</value>
        public ICreature Attacker { get; }
    }
}