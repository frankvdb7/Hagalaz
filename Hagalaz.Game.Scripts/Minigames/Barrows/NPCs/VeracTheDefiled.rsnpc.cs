using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    [NpcScriptMetaData([2030])]
    public class VeracTheDefiled : BarrowBrother
    {
        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            var hitTruPrayer = RandomStatic.Generator.NextDouble() >= 0.85;
            var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
            var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
            Owner.Combat.PerformAttack(new AttackParams()
            {
                Target = target, DamageType = hitTruPrayer ? DamageType.FullMelee : DamageType.StandardMelee, Damage = damage, MaxDamage = maxDamage,
            });
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Crush;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAccurate;
    }
}