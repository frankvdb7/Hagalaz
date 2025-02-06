using System;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Throwing
{
    /// <summary>
    ///     Class for throwing related methods.
    /// </summary>
    public static class Throwing
    {
        /// <summary>
        ///     Gets the throwing graphic.
        /// </summary>
        /// <param name="ammo">The ammo.</param>
        /// <returns></returns>
        private static short GetThrowingProjectileGraphic(IItem ammo)
        {
            var id = ammo.Id;


            return -1;
        }

        /// <summary>
        ///     Perform's standart throwing attack.
        /// </summary>
        public static void PerformThrowingStandardAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var ammo = attacker.Equipment[EquipmentSlot.Weapon];
            if (ammo == null || ammo != item)
            {
                attacker.Combat.CancelTarget();
                return;
            }

            var combat = (ICharacterCombat)attacker.Combat;

            item.EquipmentScript.RenderAttack(item, attacker, false); // might throw NotImplementedException 
            //attacker.QueueGraphic(Graphic.Create(Arrows.GetArrowPullBackGraphic(arrow), 0, 100));


            var delay = (byte)Math.Max(10, (int)Location.GetDistance(attacker.Location.X, attacker.Location.Y, victim.Location.X, victim.Location.Y) * 5);

            var projectile = new Projectile(GetThrowingProjectileGraphic(ammo));
            projectile.SetSenderData(attacker, 38, false);
            projectile.SetReceiverData(victim, 36);
            projectile.SetFlyingProperties(41, delay, 5, 11, false);
            projectile.Display();

            var max = combat.GetRangedMaxHit(victim, false);
            var preDmg = combat.GetRangedDamage(victim, false);
            combat.PerformSoulSplit(victim, preDmg);
            preDmg = victim.Combat.IncomingAttack(attacker, DamageType.FullRange, preDmg, (byte)(delay + 41));
            combat.AddRangedExperience(preDmg);

            Ammo.RemoveAmmo(attacker, ammo, victim.Location.Clone(), CreatureHelper.CalculateTicksForClientTicks(delay + 41));
            attacker.QueueTask(new RsTask(() =>
                {
                    var soaked = -1;
                    var damage = victim.Combat.Attack(attacker, DamageType.FullRange, preDmg, ref soaked);
                    var splat = new HitSplat(attacker);
                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage, damage == -1 ? 0 : damage, max <= damage);
                    if (soaked != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                    }

                    victim.QueueHitSplat(splat);
                }, CreatureHelper.CalculateTicksForClientTicks(delay + 41)));
        }
    }
}