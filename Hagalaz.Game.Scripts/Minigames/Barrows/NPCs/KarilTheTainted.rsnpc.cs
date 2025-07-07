using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([2028])]
    public class KarilTheTainted : BarrowBrother
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public KarilTheTainted(IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();

            var delay = Math.Max(10, (int)Location.GetDistance(Owner.Location.X, Owner.Location.Y, target.Location.X, target.Location.Y) * 5);

            _projectileBuilder
                .Create()
                .WithGraphicId(27)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithDelay(41)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            var dmg = ((INpcCombat)Owner.Combat).GetRangeDamage(target);
            var special = RandomStatic.Generator.NextDouble() >= 0.90;
            if (special)
            {
                target.QueueGraphic(Graphic.Create(401, CreatureHelper.CalculateTicksForClientTicks(delay)));
            }

            var handle = Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = dmg,
                DamageType = DamageType.StandardRange,
                Target = target,
                MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                Delay = delay,
            });

            handle.RegisterResultHandler(result =>
            {
                if (!result.DamageLifePoints.Succeeded)
                {
                    return;
                }

                if (!special)
                {
                    return;
                }

                if (target is not ICharacter character)
                {
                    return;
                }

                character.Statistics.DamageSkill(StatisticsConstants.Agility, (int)(character.Statistics.GetSkillLevel(StatisticsConstants.Agility) * 0.20));
                character.SendChatMessage("You feel less agile.");
            });
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Ranged;

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 7;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.RangedAccurate;

        /// <summary>
        ///     Get's attack speed of this npc.
        ///     By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackSpeed() => 6;
    }
}