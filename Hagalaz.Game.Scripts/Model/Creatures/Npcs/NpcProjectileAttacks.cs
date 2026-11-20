using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    ///     Looks of an NPC projectile: its graphic and launch timing/geometry. Duration comes from the attack's delay.
    /// </summary>
    internal readonly record struct NpcProjectile(int GraphicId, int LaunchDelay, int FromHeight = 35, int ToHeight = 35, int Angle = 180);

    /// <summary>
    ///     Single-target ranged and magic NPC attacks that fire a projectile and queue the hit: the shared skeleton of
    ///     many NPC scripts. What differs per NPC (projectile look, magic base damage, timing) is passed in; NPC
    ///     behaviour such as attack animations and target selection stays in the scripts.
    /// </summary>
    internal static class NpcProjectileAttacks
    {
        public static void Ranged(INpc owner, IProjectileBuilder projectileBuilder, ICreature target, NpcProjectile projectile, int baseDelay = 20)
        {
            var combat = (INpcCombat)owner.Combat;
            var delay = Launch(owner, projectileBuilder, target, projectile, baseDelay);
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetRangeDamage(target),
                MaxDamage = combat.GetRangeMaxHit(target),
                Delay = delay,
                DamageType = DamageType.StandardRange,
                Target = target.Handle
            });
        }

        public static void Magic(INpc owner, IProjectileBuilder projectileBuilder, ICreature target, NpcProjectile projectile, int baseDamage, int baseDelay = 20)
        {
            var combat = (INpcCombat)owner.Combat;
            var delay = Launch(owner, projectileBuilder, target, projectile, baseDelay);
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetMagicDamage(target, baseDamage),
                MaxDamage = combat.GetMagicMaxHit(target, baseDamage),
                Delay = delay,
                DamageType = DamageType.StandardMagic,
                Target = target.Handle
            });
        }

        private static int Launch(INpc owner, IProjectileBuilder projectileBuilder, ICreature target, NpcProjectile projectile, int baseDelay)
        {
            var delay = NpcAttackDelay.FromDistance(owner, target, baseDelay);
            projectileBuilder.Create()
                .WithGraphicId(projectile.GraphicId)
                .FromCreature(owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithAngle(projectile.Angle)
                .WithDelay(projectile.LaunchDelay)
                .WithFromHeight(projectile.FromHeight)
                .WithToHeight(projectile.ToHeight)
                .Send();
            return delay;
        }
    }
}
