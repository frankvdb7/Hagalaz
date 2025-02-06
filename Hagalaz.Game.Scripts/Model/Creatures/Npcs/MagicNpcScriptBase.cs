using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

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
            var delay = (byte)(20 + deltaX * 5 + deltaY * 5);

            RenderProjectile(target, delay);

            var dmg = GetMagicDamage(target);
            dmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

            Owner.QueueTask(new RsTask(() =>
            {
                var soak = -1;
                var damage = target.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                var splat = new HitSplat(Owner);
                splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, GetMagicMaxHit(target) <= damage);
                if (soak != -1)
                    splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                target.QueueHitSplat(splat);
            }, CreatureHelper.CalculateTicksForClientTicks(delay)));
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
        public virtual void RenderProjectile(ICreature target, byte delay) { }

        /// <summary>
        /// Renders the target attack.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void RenderTargetAttack(ICreature target) { }
    }
}