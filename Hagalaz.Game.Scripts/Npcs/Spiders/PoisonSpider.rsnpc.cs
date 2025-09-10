using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Spiders
{
    /// <summary>
    ///     Contains poison spider script.
    /// </summary>
    [NpcScriptMetaData([134, 1009])]
    public class PoisonSpider : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(5327));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(5328, delay));

        /// <summary>
        ///     Render's this npc death.
        /// </summary>
        /// <returns></returns>
        public override int RenderDeath()
        {
            Owner.QueueAnimation(Animation.Create(5329));
            return 5;
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => 1;

        /// <summary>
        ///     Get's attack speed of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => 4;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            var canPoison = true;
            if (target is ICharacter)
            {
                canPoison = !(target as ICharacter).Statistics.Poisoned;
            }
            else if (target is INpc)
            {
                canPoison = !(target as INpc).Statistics.Poisoned;
            }

            if (canPoison)
            {
                var myAttackLevel = Owner.Combat.GetAttackLevel();
                var enemyDefenceLevel = target.Combat.GetDefenceLevel();

                if (GetAttackStyle() == AttackStyle.MeleeAccurate)
                {
                    myAttackLevel += 3;
                }
                else if (GetAttackStyle() == AttackStyle.MeleeControlled)
                {
                    myAttackLevel++;
                }

                if (target.Combat.GetAttackStyle() == AttackStyle.MeleeDefensive)
                {
                    enemyDefenceLevel += 3;
                }
                else if (target.Combat.GetAttackStyle() == AttackStyle.MeleeControlled)
                {
                    enemyDefenceLevel += 1;
                }

                double effectiveAttack = myAttackLevel;
                effectiveAttack += 8.0;
                effectiveAttack += Owner.Combat.GetAttackBonus();
                effectiveAttack = Math.Round(effectiveAttack);


                double effectiveDefence = enemyDefenceLevel;
                effectiveDefence += 8.0;
                effectiveDefence += target.Combat.GetDefenceBonus(GetAttackBonusType());
                effectiveDefence = Math.Round(effectiveDefence);


                double attackersRoll = (int)Math.Round(effectiveAttack * (1.0 + Owner.Combat.GetAttackBonus() / 16.0) * 10.0); // / 64
                double defendersRoll = (int)Math.Round(effectiveDefence * (1.0 + target.Combat.GetDefenceBonus(GetAttackBonusType()) / 64.0) * 10.0);

                var accuracy = 0.5;
                if (attackersRoll < defendersRoll)
                {
                    accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
                }
                else if (attackersRoll > defendersRoll)
                {
                    accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);
                }

                if (RandomStatic.Generator.NextDouble() <= accuracy)
                {
                    target.Poison(62);
                }
            }

            base.PerformAttack(target);
        }

        /// <summary>
        ///     Get's attack style of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAccurate;

        /// <summary>
        ///     Get's attack bonus of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Stab;
    }
}