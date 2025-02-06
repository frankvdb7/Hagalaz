using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    /// <summary>
    ///     Class for misc methods related to bolts and bolts equipment script.
    /// </summary>
    public class Bolts : EquipmentScript
    {
        /// <summary>
        ///     Bronze bolt ids.
        /// </summary>
        public static readonly int[] Bronze = [877, 878, 6061, 6062, 879, 9236];

        /// <summary>
        ///     Blurite bolt ids.
        /// </summary>
        public static readonly int[] Blurite = [9139, 9286, 9293, 9300, 9335, 9237];

        /// <summary>
        ///     Silver bolt ids.
        /// </summary>
        public static readonly int[] Silver = [9145, 9292, 9299, 9306];

        /// <summary>
        ///     Iron bolt ids.
        /// </summary>
        public static readonly int[] Iron = [9140, 9287, 9294, 9301, 880, 9238];

        /// <summary>
        ///     Steel bolt ids.
        /// </summary>
        public static readonly int[] Steel = [9141, 9288, 9295, 9302, 9336, 9239];

        /// <summary>
        ///     Black bolt ids.
        /// </summary>
        public static readonly int[] Black = [13083, 13084, 13085, 13086];

        /// <summary>
        ///     Mithril bolt ids.
        /// </summary>
        public static readonly int[] Mithril = [9142, 9289, 9296, 9303, 9337, 9240, 9338, 9241];

        /// <summary>
        ///     Adamantine bolt ids.
        /// </summary>
        public static readonly int[] Adamantine = [9143, 9290, 9297, 9304, 9339, 9242, 9340, 9243];

        /// <summary>
        ///     Runite bolt ids.
        /// </summary>
        public static readonly int[] Runite = [9144, 9291, 9298, 9305, 9341, 9244, 9342, 9245];

        /// <summary>
        ///     Bone bolts id.
        /// </summary>
        public static readonly int Bone = 8882;

        /// <summary>
        ///     Barbed bolts id.
        /// </summary>
        public static readonly int Barbed = 881;

        /// <summary>
        ///     Mithril grapple id.
        /// </summary>
        public static readonly int MithrilGrapple = 9419;

        /// <summary>
        ///     Bolt rack id.
        /// </summary>
        public static readonly int BoltRack = 4740;

        /// <summary>
        ///     Broad tipped bolts id.
        /// </summary>
        public static readonly int BroadTipped = 13480;

        /// <summary>
        ///     Standart kebbit bolts id.
        /// </summary>
        public static readonly int Kebbit = 10158;

        /// <summary>
        ///     Long kebbit bolts id.
        /// </summary>
        public static readonly int LongKebbit = 10159;

        /// <summary>
        ///     Abyssalbane bolts ids.
        /// </summary>
        public static readonly int[] Abyssalbane = [21675, 21701, 21702, 21703];

        /// <summary>
        ///     Basiliskbane bolts ids.
        /// </summary>
        public static readonly int[] Basiliskbane = [21670, 21687, 21688, 21689];

        /// <summary>
        ///     Dragonbane bolts ids.
        /// </summary>
        public static readonly int[] Dragonbane = [21660, 21680, 21681, 21682];

        /// <summary>
        ///     Wallasalkibane bolts ids.
        /// </summary>
        public static readonly int[] Wallasalkibane = [21665, 21694, 21695, 21696];

        /// <summary>
        ///     Get's character bolt type.
        ///     This method looks character equipment ammo slot.
        /// </summary>
        /// <returns></returns>
        public static BoltType GetBoltType(ICharacter character)
        {
            var ammo = character.Equipment[EquipmentSlot.Arrow];
            if (ammo == null)
            {
                return BoltType.None;
            }

            var itemID = ammo.Id;

            if (Lookup(itemID, Bronze))
            {
                return BoltType.Bronze;
            }

            if (Lookup(itemID, Blurite))
            {
                return BoltType.Blurite;
            }

            if (Lookup(itemID, Silver))
            {
                return BoltType.Silver;
            }

            if (Lookup(itemID, Iron))
            {
                return BoltType.Iron;
            }

            if (Lookup(itemID, Steel))
            {
                return BoltType.Steel;
            }

            if (Lookup(itemID, Black))
            {
                return BoltType.Black;
            }

            if (Lookup(itemID, Mithril))
            {
                return BoltType.Mithril;
            }

            if (Lookup(itemID, Adamantine))
            {
                return BoltType.Adamantine;
            }

            if (Lookup(itemID, Runite))
            {
                return BoltType.Runite;
            }

            if (itemID == Bone)
            {
                return BoltType.Bone;
            }

            if (itemID == Barbed)
            {
                return BoltType.Barbed;
            }

            if (itemID == MithrilGrapple)
            {
                return BoltType.MithrilGrapple;
            }

            if (itemID == BoltRack)
            {
                return BoltType.BoltRack;
            }

            if (itemID == BroadTipped)
            {
                return BoltType.BroadTipped;
            }

            if (itemID == Kebbit)
            {
                return BoltType.Kebbit;
            }

            if (itemID == LongKebbit)
            {
                return BoltType.LongKebbit;
            }

            if (Lookup(itemID, Abyssalbane))
            {
                return BoltType.Abyssalbane;
            }

            if (Lookup(itemID, Basiliskbane))
            {
                return BoltType.Basiliskbane;
            }

            if (Lookup(itemID, Dragonbane))
            {
                return BoltType.Dragonbane;
            }

            if (Lookup(itemID, Wallasalkibane))
            {
                return BoltType.Wallasalkibane;
            }

            return BoltType.None;
        }

        /// <summary>
        ///     Checks if array contains given value.
        /// </summary>
        private static bool Lookup(int v, int[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] == v)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Make's one array from given arrays.
        /// </summary>
        /// <returns></returns>
        private static int[] MakeArray(params int[][] arrays)
        {
            var total = 0;
            for (var i = 0; i < arrays.Length; i++)
            {
                total += arrays[i].Length;
            }

            var array = new int[total];
            total = 0;
            for (var i = 0; i < arrays.Length; i++)
            for (var a = 0; a < arrays[i].Length; a++)
            {
                array[total++] = arrays[i][a];
            }

            return array;
        }

        /// <summary>
        ///     Happens when bolts are equiped for this character.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            if (item.Id == 9236) // OPAL
            {
                character.AddState(new State(StateType.EnchantedOpalBoltsEquipped, int.MaxValue));
            }
            else if (item.Id == 9243) // DIAMOND
            {
                character.AddState(new State(StateType.EnchantedDiamondBoltsEquipped, int.MaxValue));
            }
            else if (item.Id == 9244) // DRAGON
            {
                character.AddState(new State(StateType.EnchantedDragonstoneBoltsEquiped, int.MaxValue));
            }
            else if (item.Id == 9245) // ONYX
            {
                character.AddState(new State(StateType.EnchantedOnyxBoltsEquiped, int.MaxValue));
            }

            character.AddState(new State(StateType.BoltsEquiped, int.MaxValue));
        }

        /// <summary>
        ///     Happens when bolts are unequiped for this character.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="character"></param>
        public override void OnUnequiped(IItem item, ICharacter character)
        {
            if (item.Id == 9236) // OPAL
            {
                character.RemoveState(StateType.EnchantedOpalBoltsEquipped);
            }
            else if (item.Id == 9243) // DIAMOND
            {
                character.RemoveState(StateType.EnchantedDiamondBoltsEquipped);
            }
            else if (item.Id == 9244) // DRAGON
            {
                character.RemoveState(StateType.EnchantedDragonstoneBoltsEquiped);
            }
            else if (item.Id == 9245) // ONYX
            {
                character.RemoveState(StateType.EnchantedOnyxBoltsEquiped);
            }

            character.RemoveState(StateType.BoltsEquiped);
        }

        /// <summary>
        ///     Decides wheter character should perform bolt special attack.
        /// </summary>
        /// <param name="attacker">Character which is performing attack.</param>
        /// <param name="victim">Victim of the attack.</param>
        /// <param name="ammo">Bolt item in character's inventory.</param>
        /// <returns></returns>
        public static bool ShouldPerformSpecial(ICharacter attacker, ICreature victim, IItem ammo)
        {
            var itemID = ammo.Id;
            double chance = 0;
            switch (itemID)
            {
                // opal
                case 9236:
                    chance = 0.09;
                    break;
                // jade TODO
                case 9237:
                    chance = 0;
                    break;
                // pearl
                case 9238:
                    chance = 0.10;
                    break;
                // topaz
                case 9239:
                    chance = 0.11;
                    break;
                // sapphire
                case 9240:
                    chance = 0.9;
                    break;
                // emerald
                case 9241:
                    chance = 0.40;
                    break;
                // ruby
                case 9242:
                    chance = 0.15;
                    break;
                // diamond
                case 9243:
                    chance = 0.12;
                    break;
                // dragon
                case 9244:
                    chance = 0.10;
                    break;
                // onyx
                case 9245:
                    chance = 0.7;
                    break;
                // bolt rack (karil)
                case 4740:
                    chance = 0.2;
                    break;
                default:
                    return false;
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
        /// <param name="afterAddition">The after addition.</param>
        /// <returns></returns>
        public static bool PerformSpecial(ICharacter attacker, ICreature victim, IItem bolt, int predictedDamage, ref int afterAddition)
        {
            afterAddition = 0;
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
                case 9237:
                    return false;
                // pearl
                case 9238:
                    victim.QueueGraphic(Graphic.Create(750, 0, 1));
                    return true;
                // topaz
                case 9239 when victim is not ICharacter:
                    return false;
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
                case 9240 when victim is not ICharacter:
                    return false;
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
                case 9241 when !victim.Poison(58):
                    return false;
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
                    afterAddition = drainamt;
                    victim.QueueGraphic(Graphic.Create(754, 0, 1));
                    return true;
                }
                // diamond TODO
                case 9243:
                    victim.QueueGraphic(Graphic.Create(758, 0, 1));
                    return true;
                // dragon
                case 9244 when victim.HasState(StateType.NpcTypeDragon) || victim.HasState(StateType.AntiDragonfirePotion) && victim.HasState(StateType.AntiDragonfireShield) || victim.HasState(StateType.SuperAntiDragonfirePotion):
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
                case 9740 when !(victim is ICharacter) || !attacker.HasState(StateType.KarilTaint):
                    return false;
                case 9740:
                {
                    victim.QueueGraphic(Graphic.Create(401));
                    var character = (ICharacter)victim;
                    character.Statistics.DamageSkill(StatisticsConstants.Agility, (byte)(character.Statistics.GetSkillLevel(StatisticsConstants.Agility) * 0.20));
                    character.SendChatMessage("You feel less agile.");
                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Get's items suitable for this equipment script.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<int>  GetSuitableItems() => MakeArray(Bronze, Blurite, Silver, Iron, Steel, Black, Mithril, Adamantine, Runite, [Bone, Barbed, BoltRack, BroadTipped, Kebbit, LongKebbit
        ], Abyssalbane, Basiliskbane, Dragonbane, Wallasalkibane);
    }
}