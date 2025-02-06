using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    /// <summary>
    /// </summary>
    public class KarilTheTainted : BarrowBrother
    {
        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();

            var delay = Math.Max(10, (int)Location.GetDistance(Owner.Location.X, Owner.Location.Y, target.Location.X, target.Location.Y) * 5);

            var projectile = new Projectile(27);
            projectile.SetSenderData(Owner, 38, false);
            projectile.SetReceiverData(target, 36);
            projectile.SetFlyingProperties(41, delay, 5, 11, false);
            projectile.Display();

            var dmg = ((INpcCombat)Owner.Combat).GetRangeDamage(target);
            dmg = target.Combat.IncomingAttack(Owner, DamageType.StandardRange, dmg, delay);

            var special = RandomStatic.Generator.NextDouble() >= 0.90;
            if (special)
            {
                target.QueueGraphic(Graphic.Create(401, CreatureHelper.CalculateTicksForClientTicks(delay)));
            }

            Owner.QueueTask(new RsTask(() =>
                {
                    if (special)
                    {
                        if (target is ICharacter character)
                        {
                            character.Statistics.DamageSkill(StatisticsConstants.Agility, (byte)(character.Statistics.GetSkillLevel(StatisticsConstants.Agility) * 0.20));
                            character.SendChatMessage("You feel less agile.");
                        }
                    }

                    var soak = -1;
                    var damage = target.Combat.Attack(Owner, DamageType.StandardRange, dmg, ref soak);
                    var splat = new HitSplat(Owner);
                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetRangeMaxHit(target) <= damage);
                    if (soak != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                    }

                    target.QueueHitSplat(splat);
                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
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

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [2028];
    }
}