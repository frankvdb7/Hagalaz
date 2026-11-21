using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <inheritdoc />
    public sealed class NpcAttackService(IProjectileBuilder projectileBuilder) : INpcAttackService
    {
        /// <inheritdoc />
        public void PerformRanged(INpc owner, ICreature target, int baseDelay = 20) =>
            QueueRanged(owner, target, NpcAttackDelay.FromDistance(owner, target, baseDelay));

        /// <inheritdoc />
        public void PerformMagic(INpc owner, ICreature target, int baseDamage, int baseDelay = 20) =>
            QueueMagic(owner, target, baseDamage, NpcAttackDelay.FromDistance(owner, target, baseDelay));

        /// <inheritdoc />
        public void PerformRanged(INpc owner, ICreature target, NpcProjectile projectile, int baseDelay = 20)
        {
            var delay = NpcAttackDelay.FromDistance(owner, target, baseDelay);
            Launch(owner, target, projectile, delay);
            QueueRanged(owner, target, delay);
        }

        /// <inheritdoc />
        public void PerformMagic(INpc owner, ICreature target, NpcProjectile projectile, int baseDamage, int baseDelay = 20)
        {
            var delay = NpcAttackDelay.FromDistance(owner, target, baseDelay);
            Launch(owner, target, projectile, delay);
            QueueMagic(owner, target, baseDamage, delay);
        }

        private static void QueueRanged(INpc owner, ICreature target, int delay)
        {
            var combat = (INpcCombat)owner.Combat;
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetRangeDamage(target),
                MaxDamage = combat.GetRangeMaxHit(target),
                Delay = delay,
                DamageType = DamageType.StandardRange,
                Target = target.Handle
            });
        }

        private static void QueueMagic(INpc owner, ICreature target, int baseDamage, int delay)
        {
            var combat = (INpcCombat)owner.Combat;
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetMagicDamage(target, baseDamage),
                MaxDamage = combat.GetMagicMaxHit(target, baseDamage),
                Delay = delay,
                DamageType = DamageType.StandardMagic,
                Target = target.Handle
            });
        }

        private void Launch(INpc owner, ICreature target, NpcProjectile projectile, int duration) =>
            projectileBuilder.Create()
                .WithGraphicId(projectile.GraphicId)
                .FromCreature(owner)
                .ToCreature(target)
                .WithDuration(duration)
                .WithAngle(projectile.Angle)
                .WithDelay(projectile.LaunchDelay)
                .WithFromHeight(projectile.FromHeight)
                .WithToHeight(projectile.ToHeight)
                .Send();
    }
}
