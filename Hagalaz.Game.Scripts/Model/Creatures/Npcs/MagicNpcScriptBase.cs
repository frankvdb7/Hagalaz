using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class MagicNpcScriptBase : NpcScriptBase
    {
        /// <summary>
        /// Get's attack bonus type of this npc.
        /// By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        /// AttackBonus.
        /// </returns>
        public sealed override AttackBonus GetAttackBonusType() => AttackBonus.Magic;

        /// <summary>
        /// Get's attack distance of this npc.
        /// By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        /// System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 8;

        /// <summary>
        /// Get's attack style of this npc.
        /// By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        /// AttackStyle.
        /// </returns>
        public sealed override AttackStyle GetAttackStyle() => AttackStyle.MagicNormal;

        /// <summary>
        /// Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public sealed override void PerformAttack(ICreature target)
        {
            RenderAttack();
            RenderTargetAttack(target);

            var deltaX = Owner.Location.X - target.Location.X;
            var deltaY = Owner.Location.Y - target.Location.Y;
            if (deltaX < 0)
                deltaX = -deltaX;
            if (deltaY < 0)
                deltaY = -deltaY;
            var delay = 20 + deltaX * 5 + deltaY * 5;

            RenderProjectile(target, delay);

            var damage = GetMagicDamage(target);
            var maxDamage = GetMagicMaxHit(target);
            target.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                MaxDamage = maxDamage,
                Target = target,
                DamageType = DamageType.StandardMagic,
                Delay = delay,
            });
        }

        /// <summary>
        /// Gets the magic damage.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public abstract int GetMagicDamage(ICreature target);

        /// <summary>
        /// Gets the magic maximum hit.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public abstract int GetMagicMaxHit(ICreature target);

        /// <summary>
        /// Renders the projectile.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="delay">The delay.</param>
        public virtual void RenderProjectile(ICreature target, int delay) { }

        /// <summary>
        /// Renders the target attack.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void RenderTargetAttack(ICreature target) { }
    }
}