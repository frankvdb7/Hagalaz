using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common;
using Hagalaz.Game.Utilities;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    /// <summary>
    ///     Class for crossbow related methods.
    /// </summary>
    public class CrossbowLogicService : ICrossbowLogicService
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public CrossbowLogicService(IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     Get's character crossbow type.
        ///     This method looks character equipment weapon slot.
        /// </summary>
        /// <returns></returns>
        private CrossbowType GetCrossbowType(ICharacter character)
        {
            var weapon = character.Equipment[EquipmentSlot.Weapon];
            if (weapon == null)
            {
                return CrossbowType.None;
            }

            var itemID = weapon.Id;
            switch (itemID)
            {
                case 9174: return CrossbowType.Bronze;
                case 9176: return CrossbowType.Blurite;
                case 9177: return CrossbowType.Iron;
                case 9179: return CrossbowType.Steel;
                case 13081: return CrossbowType.Black;
                case 9181: return CrossbowType.Mithril;
                case 9183: return CrossbowType.Adamant;
                case 9185:
                case 13530:
                    return CrossbowType.Rune;
                case 837: return CrossbowType.Crossbow;
                case 767:
                case 11165:
                case 11167:
                    return CrossbowType.Phoenix;
                case 8880: return CrossbowType.Dorgeshuun;
                case 4734:
                case 4934:
                case 4935:
                case 4936:
                case 4937:
                case 4938:
                    return CrossbowType.Karils;
                case 10156: return CrossbowType.Hunters;
                case 14684: return CrossbowType.Zaniks;
                case 18357:
                case 18358:
                    return CrossbowType.Chaotic;
                default: return CrossbowType.None;
            }
        }

        /// <summary>
        ///     Get's suitable bolt types for given crossbow.
        /// </summary>
        /// <returns></returns>
        private BoltType[] GetSuitableBoltTypes(CrossbowType crossbow)
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
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black, BoltType.Mithril,
                        BoltType.Adamantine
                    ];
                case CrossbowType.Rune:
                case CrossbowType.Chaotic:
                    return
                    [
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black, BoltType.Mithril,
                        BoltType.Adamantine, BoltType.Runite, BoltType.BroadTipped
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
                        BoltType.Bronze, BoltType.Barbed, BoltType.Blurite, BoltType.Silver, BoltType.Iron, BoltType.Steel, BoltType.Black, BoltType.Mithril,
                        BoltType.Adamantine, BoltType.Bone
                    ];
                default: return [];
            }
        }

        /// <summary>
        ///     Check's if character has suitable crossbow ammo.
        ///     In some cases of false it sends messages such as "There's no ammo left in your quiver."
        /// </summary>
        /// <returns></returns>
        private bool HasSuitableCrossbowAmmo(ICharacter character, int amount = 1)
        {
            var ammo = character.Equipment[EquipmentSlot.Arrow];
            if (ammo == null || ammo.Count <= 0)
            {
                character.SendChatMessage("There's no ammo left in your quiver.");
                return false;
            }

            if (ammo.Count < amount)
            {
                character.SendChatMessage("There's not enough ammo left in your quiver.");
                return false;
            }

            var type = GetCrossbowType(character);
            if (type == CrossbowType.None)
            {
                return false;
            }

            var bolt = GetBoltType(character);
            if (bolt == BoltType.None || !GetSuitableBoltTypes(type).Contains(bolt))
            {
                character.SendChatMessage("This ammo is not suitable with your weapon.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Perform's crossbow attack.
        /// </summary>
        public void PerformCrossbowAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var ammo = attacker.Equipment[EquipmentSlot.Arrow];
            if (!HasSuitableCrossbowAmmo(attacker) || ammo == null)
            {
                attacker.Combat.CancelTarget();
                return;
            }

            var combat = (ICharacterCombat)attacker.Combat;

            item.EquipmentScript.RenderAttack(item, attacker, false); // might throw NotImplementedException 


            var delay = Math.Max(10, (int)Location.GetDistance(attacker.Location.X, attacker.Location.Y, victim.Location.X, victim.Location.Y) * 5);

            _projectileBuilder.Create()
                .WithGraphicId(27)
                .FromCreature(attacker)
                .ToCreature(victim)
                .WithDuration(delay)
                .WithDelay(41)
                .WithFromHeight(38)
                .WithToHeight(36)
                .WithSlope(5)
                .WithAngle(11)
                .Send();

            var spec = ShouldPerformSpecial(attacker, victim, ammo);

            var damage = combat.GetRangedDamage(victim, spec);
            var maxDamage = combat.GetRangedMaxHit(victim, spec);
            var attackResult = attacker.Combat.PerformAttack(new AttackParams
            {
                Target = victim,
                DamageType = DamageType.FullRange,
                Damage = damage,
                MaxDamage = maxDamage,
                Delay = delay
            });

            if (spec)
            {
                attackResult.RegisterResultHandler(result => { PerformSpecial(attacker, victim, ammo, result.Damage.Count); });
            }

            Ammo.RemoveAmmo(attacker, ammo, victim.Location.Clone(), CreatureHelper.CalculateTicksForClientTicks(delay + 41));
        }

        /// <summary>
        ///     Get's character bolt type.
        ///     This method looks character equipment ammo slot.
        /// </summary>
        /// <returns></returns>
        private BoltType GetBoltType(ICharacter character)
        {
            var ammo = character.Equipment[EquipmentSlot.Arrow];
            if (ammo == null)
            {
                return BoltType.None;
            }

            var itemID = ammo.Id;

            // First check individual item IDs
            var boltType = itemID switch
            {
                _ when itemID == Bolts.Bone => BoltType.Bone,
                _ when itemID == Bolts.Barbed => BoltType.Barbed,
                _ when itemID == Bolts.MithrilGrapple => BoltType.MithrilGrapple,
                _ when itemID == Bolts.BoltRack => BoltType.BoltRack,
                _ when itemID == Bolts.BroadTipped => BoltType.BroadTipped,
                _ when itemID == Bolts.Kebbit => BoltType.Kebbit,
                _ when itemID == Bolts.LongKebbit => BoltType.LongKebbit,
                _ => BoltType.None
            };

            if (boltType != BoltType.None)
            {
                return boltType;
            }

            // Then check arrays
            return itemID switch
            {
                _ when Bolts.Bronze.Contains(itemID) => BoltType.Bronze,
                _ when Bolts.Blurite.Contains(itemID) => BoltType.Blurite,
                _ when Bolts.Silver.Contains(itemID) => BoltType.Silver,
                _ when Bolts.Iron.Contains(itemID) => BoltType.Iron,
                _ when Bolts.Steel.Contains(itemID) => BoltType.Steel,
                _ when Bolts.Black.Contains(itemID) => BoltType.Black,
                _ when Bolts.Mithril.Contains(itemID) => BoltType.Mithril,
                _ when Bolts.Adamantine.Contains(itemID) => BoltType.Adamantine,
                _ when Bolts.Runite.Contains(itemID) => BoltType.Runite,
                _ when Bolts.Abyssalbane.Contains(itemID) => BoltType.Abyssalbane,
                _ when Bolts.Basiliskbane.Contains(itemID) => BoltType.Basiliskbane,
                _ when Bolts.Dragonbane.Contains(itemID) => BoltType.Dragonbane,
                _ when Bolts.Wallasalkibane.Contains(itemID) => BoltType.Wallasalkibane,
                _ => BoltType.None
            };
        }

        /// <summary>
        ///     Decides wheter character should perform bolt special attack.
        /// </summary>
        /// <param name="attacker">Character which is performing attack.</param>
        /// <param name="victim">Victim of the attack.</param>
        /// <param name="ammo">Bolt item in character's inventory.</param>
        /// <returns></returns>
        private bool ShouldPerformSpecial(ICharacter attacker, ICreature victim, IItem ammo)
        {
            var itemID = ammo.Id;
            double chance = 0;
            switch (itemID)
            {
                // opal
                case 9236: chance = 0.09; break;
                // jade TODO
                case 9237: chance = 0; break;
                // pearl
                case 9238: chance = 0.10; break;
                // topaz
                case 9239: chance = 0.11; break;
                // sapphire
                case 9240: chance = 0.9; break;
                // emerald
                case 9241: chance = 0.40; break;
                // ruby
                case 9242: chance = 0.15; break;
                // diamond
                case 9243: chance = 0.12; break;
                // dragon
                case 9244: chance = 0.10; break;
                // onyx
                case 9245: chance = 0.7; break;
                // bolt rack (karil)
                case 4740: chance = 0.2; break;
                default: return false;
            }

            if (RandomStatic.Generator.NextDouble() > chance)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Perform's bolt special attack if character is lucky.
        ///     Returns true if performed special attack.
        /// </summary>
        /// <param name="attacker">Character which is performing attack.</param>
        /// <param name="victim">Victim of the attack.</param>
        /// <param name="bolt">Bolt item in character's inventory.</param>
        /// <param name="predictedDamage">The predicted damage.</param>
        /// <returns></returns>
        private bool PerformSpecial(ICharacter attacker, ICreature victim, IItem bolt, int predictedDamage)
        {
            if (predictedDamage == -1)
            {
                return false;
            }

            var itemID = bolt.Id;

            switch (itemID)
            {
                // opal
                case 9236:
                    victim.QueueGraphic(Graphic.Create(749, 0, 1));
                    return true;
                // jade TODO
                case 9237: return false;
                // pearl
                case 9238:
                    victim.QueueGraphic(Graphic.Create(750, 0, 1));
                    return true;
                // topaz
                case 9239 when victim is not ICharacter: return false;
                case 9239:
                    {
                        var v = (ICharacter)victim;
                        var maxLow = v.Statistics.LevelForExperience(StatisticsConstants.Magic) - RandomStatic.Generator.Next(1, 6);
                        if (maxLow < 0)
                        {
                            maxLow = 0;
                        }

                        if (v.Statistics.GetSkillLevel(StatisticsConstants.Magic) <= maxLow)
                        {
                            return false;
                        }

                        if (v.Statistics.DamageSkill(StatisticsConstants.Magic, (byte)(v.Statistics.GetSkillLevel(StatisticsConstants.Magic) - maxLow)) > 0)
                        {
                            victim.QueueGraphic(Graphic.Create(757, 0, 1));
                            return true;
                        }

                        return false;
                    }
                // sapphire
                case 9240 when victim is not ICharacter: return false;
                case 9240:
                    {
                        var v = (ICharacter)victim;
                        var amount = (int)(v.Statistics.GetMaximumPrayerPoints() * (RandomStatic.Generator.Next(10, 18) / 100.0));
                        if (amount <= 0)
                        {
                            return false;
                        }

                        var drained = v.Statistics.DrainPrayerPoints(amount);
                        if (drained <= 0)
                        {
                            return false;
                        }

                        attacker.Statistics.HealPrayerPoints(drained);
                        victim.QueueGraphic(Graphic.Create(751, 0, 1));
                        return true;
                    }
                // emerald
                case 9241 when !victim.Poison(58): return false;
                case 9241:
                    victim.QueueGraphic(Graphic.Create(752, 0, 2));
                    return true;
                // ruby
                case 9242:
                    {
                        var hp = (int)(attacker.Statistics.LifePoints * 0.10);
                        if (hp >= attacker.Statistics.LifePoints || hp < 1)
                        {
                            return false;
                        }

                        int targetHitpoints;
                        if (victim is ICharacter character)
                        {
                            targetHitpoints = character.Statistics.LifePoints;
                        }
                        else
                        {
                            targetHitpoints = ((INpc)victim).Statistics.LifePoints;
                        }

                        var drainamt = (int)(targetHitpoints * 0.20);
                        if (drainamt < 1)
                        {
                            return false;
                        }

                        attacker.Statistics.DamageLifePoints(hp);
                        victim.QueueGraphic(Graphic.Create(754, 0, 1));
                        return true;
                    }
                // diamond TODO
                case 9243:
                    victim.QueueGraphic(Graphic.Create(758, 0, 1));
                    return true;
                // dragon
                case 9244 when victim.HasState<NpcTypeDragonState>() ||
                               victim.HasState<AntiDragonfirePotionState>() && victim.HasState<AntiDragonfireShieldState>() ||
                               victim.HasState<SuperAntiDragonfirePotionState>():
                    return false;
                case 9244:
                    victim.QueueGraphic(Graphic.Create(756, 0, 2));
                    return true;
                // onyx
                case 9245:
                    attacker.Statistics.HealLifePoints((int)(predictedDamage * 0.25));
                    victim.QueueGraphic(Graphic.Create(753, 0, 2));
                    return true;
                // bolt rack
                case 9740 when victim is not ICharacter || !attacker.HasState<KarilTaintState>(): return false;
                case 9740:
                    {
                        victim.QueueGraphic(Graphic.Create(401));
                        var character = (ICharacter)victim;
                        character.Statistics.DamageSkill(StatisticsConstants.Agility,
                            (byte)(character.Statistics.GetSkillLevel(StatisticsConstants.Agility) * 0.20));
                        character.SendChatMessage("You feel less agile.");
                        return true;
                    }
                default: return false;
            }
        }
    }
}