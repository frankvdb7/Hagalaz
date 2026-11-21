using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    ///     Performs the single-target ranged and magic attacks that many NPC scripts share. Scripts decide when and
    ///     whom to attack and keep their own animations; the service owns how the hit is built and timed.
    /// </summary>
    public interface INpcAttackService
    {
        /// <summary>
        ///     Queues a ranged hit from <paramref name="owner"/> on <paramref name="target"/>.
        /// </summary>
        /// <param name="owner">The attacking NPC.</param>
        /// <param name="target">The target.</param>
        /// <param name="baseDelay">Hit delay before the distance component is added.</param>
        void PerformRanged(INpc owner, ICreature target, int baseDelay = 20);

        /// <summary>
        ///     Queues a magic hit from <paramref name="owner"/> on <paramref name="target"/>.
        /// </summary>
        /// <param name="owner">The attacking NPC.</param>
        /// <param name="target">The target.</param>
        /// <param name="baseDamage">Base damage the magic hit and max hit are derived from.</param>
        /// <param name="baseDelay">Hit delay before the distance component is added.</param>
        void PerformMagic(INpc owner, ICreature target, int baseDamage, int baseDelay = 20);

        /// <summary>
        ///     Fires <paramref name="projectile"/> at <paramref name="target"/> and queues a ranged hit that lands with it.
        /// </summary>
        /// <param name="owner">The attacking NPC.</param>
        /// <param name="target">The target.</param>
        /// <param name="projectile">The projectile to fire.</param>
        /// <param name="baseDelay">Hit delay before the distance component is added.</param>
        void PerformRanged(INpc owner, ICreature target, NpcProjectile projectile, int baseDelay = 20);

        /// <summary>
        ///     Fires <paramref name="projectile"/> at <paramref name="target"/> and queues a magic hit that lands with it.
        /// </summary>
        /// <param name="owner">The attacking NPC.</param>
        /// <param name="target">The target.</param>
        /// <param name="projectile">The projectile to fire.</param>
        /// <param name="baseDamage">Base damage the magic hit and max hit are derived from.</param>
        /// <param name="baseDelay">Hit delay before the distance component is added.</param>
        void PerformMagic(INpc owner, ICreature target, NpcProjectile projectile, int baseDamage, int baseDelay = 20);
    }
}
