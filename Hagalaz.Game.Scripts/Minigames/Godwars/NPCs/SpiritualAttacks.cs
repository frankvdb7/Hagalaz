using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs
{
    /// <summary>
    ///     Attack rules shared by the Bandos and Saradomin spiritual mages and rangers.
    ///     Faction behaviour stays in each faction's script.
    /// </summary>
    internal static class SpiritualAttacks
    {
        private const int MageSpellBonus = 146;

        public static void PerformMagicAttack(INpc owner, ICreature target)
        {
            var combat = (INpcCombat)owner.Combat;
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetMagicDamage(target, MageSpellBonus),
                MaxDamage = combat.GetMagicMaxHit(target, MageSpellBonus),
                Target = target.Handle,
                Delay = NpcAttackDelay.FromDistance(owner, target),
                DamageType = DamageType.StandardMagic
            });
        }

        public static void PerformRangedAttack(INpc owner, ICreature target)
        {
            var combat = (INpcCombat)owner.Combat;
            owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = combat.GetRangeDamage(target),
                MaxDamage = combat.GetRangeMaxHit(target),
                Delay = NpcAttackDelay.FromDistance(owner, target),
                DamageType = DamageType.StandardRange,
                Target = target.Handle
            });
        }
    }
}
