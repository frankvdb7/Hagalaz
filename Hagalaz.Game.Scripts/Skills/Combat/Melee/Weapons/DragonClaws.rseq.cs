using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains dragon claws equipment script.
    /// </summary>
    public class DragonClaws : EquipmentScript
    {
        /// <summary>
        ///     Perform's special attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        public override void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var combat = (ICharacterCombat)attacker.Combat;
            attacker.QueueAnimation(Animation.Create(10961));
            attacker.QueueGraphic(Graphic.Create(1950));

            var standardMax = combat.GetMeleeMaxHit(victim, false);
            var fullMax = combat.GetMeleeMaxHit(victim, true);

            var damage1 = combat.GetMeleeDamage(victim, true);
            var damage2 = -1;
            var damage3 = -1;
            var damage4 = -1;
            if (damage1 != -1)
            {
                damage2 = (int)(damage1 * 0.50);
                damage3 = damage2 / 2;
                damage4 = damage3 + 1;
            }
            else if ((damage2 = combat.GetMeleeDamage(victim, true)) != -1)
            {
                damage3 = (int)(damage2 * 0.50);
                damage4 = (int)(damage2 * 0.50) + 1;
            }
            else if ((damage3 = combat.GetMeleeDamage(victim, true)) != -1)
            {
                damage4 = damage3;
                var max = (int)(fullMax * 0.75);
                if (damage3 > max)
                {
                    damage3 = max;
                }

                if (damage4 > max)
                {
                    damage4 = max;
                }

                damage4++;
            }
            else if ((damage4 = combat.GetMeleeDamage(victim, true)) != -1)
            {
                damage4 = (int)(damage4 * 1.5);
            }
            else
            {
                damage4 = RandomStatic.Generator.Next(0, 8);
            }

            combat.PerformSoulSplit(victim, CreatureHelper.CalculatePredictedDamage([
                damage1, damage2, damage3, damage4
            ]));

            combat.AddMeleeExperience(damage1);
            combat.AddMeleeExperience(damage2);
            combat.AddMeleeExperience(damage3);
            combat.AddMeleeExperience(damage4);

            if (damage1 == -1 && damage2 == -1 && damage3 == -1 && damage4 < 8)
            {
                damage1 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage1, 0);
                damage4 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage4, 20);

                var soak1 = -1;
                damage1 = victim.Combat.Attack(attacker, DamageType.FullMelee, -1, ref soak1);
                var splat1 = new HitSplat(attacker);
                splat1.SetFirstSplat(HitSplatType.HitMiss, 0, false);
                if (soak1 != -1)
                {
                    splat1.SetSecondSplat(HitSplatType.HitDefendedDamage, soak1, false);
                }

                victim.QueueHitSplat(splat1);
                victim.QueueTask(new RsTask(() =>
                {
                    var soak4 = -1;
                    damage4 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage4, ref soak4);
                    combat.AddMeleeExperience(damage4);
                    var splat2 = new HitSplat(attacker);
                    splat2.SetFirstSplat(damage4 > 0 ? HitSplatType.HitMeleeDamage : HitSplatType.HitMiss, damage4 > 0 ? damage4 : 0, damage4 >= standardMax);
                    if (soak4 != -1)
                    {
                        splat2.SetSecondSplat(HitSplatType.HitDefendedDamage, soak4, false);
                    }

                    victim.QueueHitSplat(splat2);
                }, 1));
            }
            else
            {
                damage1 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage1, 0);
                damage2 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage2, 0);
                damage3 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage3, 20);
                damage4 = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, damage4, 20);

                var soak1 = -1;
                var soak2 = -1;
                damage1 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage1, ref soak1);
                damage2 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage2, ref soak2);
                var splat1 = new HitSplat(attacker);
                var splat2 = new HitSplat(attacker);
                splat1.SetFirstSplat(damage1 > 0 ? HitSplatType.HitMeleeDamage : HitSplatType.HitMiss, damage1 > 0 ? damage1 : 0, damage1 >= standardMax);
                splat2.SetFirstSplat(damage2 > 0 ? HitSplatType.HitMeleeDamage : HitSplatType.HitMiss, damage2 > 0 ? damage2 : 0, damage2 >= standardMax);
                if (soak1 != -1)
                {
                    splat1.SetSecondSplat(HitSplatType.HitDefendedDamage, soak1, false);
                }

                if (soak2 != -1)
                {
                    splat2.SetSecondSplat(HitSplatType.HitDefendedDamage, soak2, false);
                }

                victim.QueueHitSplat(splat1);
                victim.QueueHitSplat(splat2);
                victim.QueueTask(new RsTask(() =>
                {
                    var soak3 = -1;
                    var soak4 = -1;
                    damage3 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage3, ref soak3);
                    damage4 = victim.Combat.Attack(attacker, DamageType.FullMelee, damage4, ref soak4);
                    var splat3 = new HitSplat(attacker);
                    var splat4 = new HitSplat(attacker);
                    splat3.SetFirstSplat(damage3 > 0 ? HitSplatType.HitMeleeDamage : HitSplatType.HitMiss, damage3 > 0 ? damage3 : 0, damage3 >= standardMax);
                    splat4.SetFirstSplat(damage4 > 0 ? HitSplatType.HitMeleeDamage : HitSplatType.HitMiss, damage4 > 0 ? damage4 : 0, damage4 >= standardMax);
                    if (soak3 != -1)
                    {
                        splat3.SetSecondSplat(HitSplatType.HitDefendedDamage, soak3, false);
                    }

                    if (soak4 != -1)
                    {
                        splat4.SetSecondSplat(HitSplatType.HitDefendedDamage, soak4, false);
                    }

                    victim.QueueHitSplat(splat3);
                    victim.QueueHitSplat(splat4);
                }, 1));
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 500;

        /// <summary>
        ///     Get's items suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<int> GetSuitableItems() =>
        [
            14484, 14486
        ];
    }
}