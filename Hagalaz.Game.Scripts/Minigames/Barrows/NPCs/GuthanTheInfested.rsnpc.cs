using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    [NpcScriptMetaData([2027])]
    public class GuthanTheInfested : BarrowBrother
    {
        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
            var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
            var handle = Owner.Combat.PerformAttack(new AttackParams
            {
                Target = target, DamageType = DamageType.StandardMelee, Damage = damage, MaxDamage = maxDamage,
            });
            handle.RegisterResultHandler(result =>
            {
                if (result.DamageLifePoints.Succeeded)
                {
                    if (RandomStatic.Generator.NextDouble() >= 0.90)
                    {
                        target.QueueGraphic(Graphic.Create(398));
                        Owner.Statistics.HealLifePoints(result.DamageLifePoints.Count);
                    }
                }
            });
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Stab;

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