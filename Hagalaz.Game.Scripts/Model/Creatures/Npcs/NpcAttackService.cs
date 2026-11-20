using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <inheritdoc />
    public sealed class NpcAttackService : INpcAttackService
    {
        /// <inheritdoc />
        public void PerformRanged(INpc owner, ICreature target, int baseDelay = 20)
        {
            var combat = (INpcCombat)owner.Combat;
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetRangeDamage(target),
                MaxDamage = combat.GetRangeMaxHit(target),
                Delay = NpcAttackDelay.FromDistance(owner, target, baseDelay),
                DamageType = DamageType.StandardRange,
                Target = target.Handle
            });
        }

        /// <inheritdoc />
        public void PerformMagic(INpc owner, ICreature target, int baseDamage, int baseDelay = 20)
        {
            var combat = (INpcCombat)owner.Combat;
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetMagicDamage(target, baseDamage),
                MaxDamage = combat.GetMagicMaxHit(target, baseDamage),
                Delay = NpcAttackDelay.FromDistance(owner, target, baseDelay),
                DamageType = DamageType.StandardMagic,
                Target = target.Handle
            });
        }
    }
}
