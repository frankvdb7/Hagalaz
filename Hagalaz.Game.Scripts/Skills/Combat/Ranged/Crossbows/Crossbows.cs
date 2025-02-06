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

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    /// <summary>
    ///     Class for crossbow related methods.
    /// </summary>
    public static class Crossbows
    {
        /// <summary>
        ///     Get's character crossbow type.
        ///     This method looks character equipment weapon slot.
        /// </summary>
        /// <returns></returns>
        public static CrossbowType GetCrossbowType(ICharacter character)
        {
            var weapon = character.Equipment[EquipmentSlot.Weapon];
            if (weapon == null)
            {
                return CrossbowType.None;
            }

            var itemID = weapon.Id;
            switch (itemID)
            {
                case 9174:
                    return CrossbowType.Bronze;
                case 9176:
                    return CrossbowType.Blurite;
                case 9177:
                    return CrossbowType.Iron;
                case 9179:
                    return CrossbowType.Steel;
                case 13081:
                    return CrossbowType.Black;
                case 9181:
                    return CrossbowType.Mithril;
                case 9183:
                    return CrossbowType.Adamant;
                case 9185:
                case 13530:
                    return CrossbowType.Rune;
                case 837:
                    return CrossbowType.Crossbow;
                case 767:
                case 11165:
                case 11167:
                    return CrossbowType.Phoenix;
                case 8880:
                    return CrossbowType.Dorgeshuun;
                case 4734:
                case 4934:
                case 4935:
                case 4936:
                case 4937:
                case 4938:
                    return CrossbowType.Karils;
                case 10156:
                    return CrossbowType.Hunters;
                case 14684:
                    return CrossbowType.Zaniks;
                case 18357:
                case 18358:
                    return CrossbowType.Chaotic;
                default:
                    return CrossbowType.None;
            }
        }

        /// <summary>
        ///     Get's suitable bolt types for given crossbow.
        /// </summary>
        /// <returns></returns>
        public static BoltType[] GetSuitableBoltTypes(CrossbowType crossbow)
        {
            switch (crossbow)
            {
                case CrossbowType.Crossbow:
                case CrossbowType.Phoenix:
                case CrossbowType.Bronze:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed
                    ];
                case CrossbowType.Blurite:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite
                    ];
                case CrossbowType.Iron:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron
                    ];
                case CrossbowType.Steel:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel
                    ];
                case CrossbowType.Black:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black
                    ];
                case CrossbowType.Mithril:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black, BoltType.Mithril
                    ];
                case CrossbowType.Adamant:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black, BoltType.Mithril, BoltType.Adamantine
                    ];
                case CrossbowType.Rune:
                case CrossbowType.Chaotic:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black, BoltType.Mithril, BoltType.Adamantine, BoltType.Runite, BoltType.BroadTipped
                    ];
                case CrossbowType.Hunters:
                    return
                    [
                        BoltType.Kebbit, BoltType.LongKebbit
                    ];
                case CrossbowType.Dorgeshuun:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Bone
                    ];
                case CrossbowType.Karils:
                    return
                    [
                        BoltType.BoltRack
                    ];
                case CrossbowType.Zaniks:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black, BoltType.Mithril, BoltType.Adamantine, BoltType.Bone
                    ];
                default:
                    return [];
            }
        }

        /// <summary>
        ///     Perform's crossbow attack.
        /// </summary>
        public static void PerformCrossbowAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var ammo = attacker.Equipment[EquipmentSlot.Arrow];
            if (!Ammo.HasSuitableCrossbowAmmo(attacker) || ammo == null)
            {
                attacker.Combat.CancelTarget();
                return;
            }

            var combat = (ICharacterCombat)attacker.Combat;

            item.EquipmentScript.RenderAttack(item, attacker, false); // might throw NotImplementedException 


            var delay = (byte)Math.Max(10, (int)Location.GetDistance(attacker.Location.X, attacker.Location.Y, victim.Location.X, victim.Location.Y) * 5);

            var projectile = new Projectile(27);
            projectile.SetSenderData(attacker, 38, false);
            projectile.SetReceiverData(victim, 36);
            projectile.SetFlyingProperties(41, delay, 5, 11, false);
            projectile.Display();

            var spec = Bolts.ShouldPerformSpecial(attacker, victim, ammo);
            var max = combat.GetRangedMaxHit(victim, false);
            var preDmg = combat.GetRangedDamage(victim, spec);
            combat.PerformSoulSplit(victim, preDmg);
            preDmg = victim.Combat.IncomingAttack(attacker, DamageType.FullRange, preDmg, (byte)(delay + 41));
            combat.AddRangedExperience(preDmg);
            var ruby = 0;
            if (spec)
            {
                Bolts.PerformSpecial(attacker, victim, ammo, preDmg, ref ruby);
            }

            preDmg += ruby;

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