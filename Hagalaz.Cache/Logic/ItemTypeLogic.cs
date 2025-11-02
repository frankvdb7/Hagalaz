using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Hagalaz.Cache.Logic
{
    /// <summary>
    /// Contains extension methods providing business logic for <see cref="IItemType"/>.
    /// This class centralizes item-related calculations and checks that were previously part of the <see cref="ItemType"/> class.
    /// </summary>
    public static class ItemTypeLogic
    {
        /// <summary>
        /// Determines whether this item has a special attack bar when equipped.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns><c>true</c> if the item has a special attack bar; otherwise, <c>false</c>.</returns>
        public static bool HasSpecialBar(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.HasSpecialBar, out var value))
            {
                if (value is int intValue && intValue == 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the quest ID required to use or see this item.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The quest ID, or -1 if no quest is required.</returns>
        public static int GetQuestId(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.QuestId, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets the attack speed of the weapon.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The attack speed value, defaulting to 4 if not specified.</returns>
        public static int GetAttackSpeed(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.AttackSpeed, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 4;
        }

        /// <summary>
        /// Gets the stab attack bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The stab attack bonus.</returns>
        public static int GetStabAttack(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.StabAttack, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the slash attack bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The slash attack bonus.</returns>
        public static int GetSlashAttack(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.SlashAttack, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the crush attack bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The crush attack bonus.</returns>
        public static int GetCrushAttack(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.CrushAttack, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the magic attack bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The magic attack bonus.</returns>
        public static int GetMagicAttack(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.MagicAttack, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the ranged attack bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The ranged attack bonus.</returns>
        public static int GetRangeAttack(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.RangeAttack, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the stab defence bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The stab defence bonus.</returns>
        public static int GetStabDefence(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.StabDefence, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the slash defence bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The slash defence bonus.</returns>
        public static int GetSlashDefence(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.SlashDefence, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the crush defence bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The crush defence bonus.</returns>
        public static int GetCrushDefence(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.CrushDefence, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the magic defence bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The magic defence bonus.</returns>
        public static int GetMagicDefence(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.MagicDefence, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the ranged defence bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The ranged defence bonus.</returns>
        public static int GetRangeDefence(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.RangeDefence, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the summoning defence bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The summoning defence bonus.</returns>
        public static int GetSummoningDefence(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.SummoningDefence, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the melee absorption bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The melee absorption bonus.</returns>
        public static int GetAbsorbMeleeBonus(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.AbsorbMeleeBonus, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the magic absorption bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The magic absorption bonus.</returns>
        public static int GetAbsorbMageBonus(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.AbsorbMageBonus, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the ranged absorption bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The ranged absorption bonus.</returns>
        public static int GetAbsorbRangeBonus(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.AbsorbRangeBonus, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the strength bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The strength bonus.</returns>
        public static int GetStrengthBonus(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.StrengthBonus, out var value))
            {
                if (value is int intValue)
                {
                    return intValue / 10;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the ranged strength bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The ranged strength bonus.</returns>
        public static int GetRangedStrengthBonus(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.RangedStrengthBonus, out var value))
            {
                if (value is int intValue)
                {
                    return intValue / 10;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the magic damage bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The magic damage bonus.</returns>
        public static int GetMagicDamage(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.MagicDamage, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the prayer bonus.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The prayer bonus.</returns>
        public static int GetPrayerBonus(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.PrayerBonus, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the weapon type ID.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The weapon type ID.</returns>
        public static int GetWeaponType(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.WeaponType, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the attack bonus types for this weapon.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>An array of attack bonus type IDs.</returns>
        public static int[] GetAttackBonusTypes(this IItemType itemType)
        {
            int weaponType = itemType.GetWeaponType();
            switch (weaponType)
            {
                case 1: return [3, 3, 3, -1];
                case 2: return [2, 2, 3, 2];
                case 3: return [3, 3, 3, -1];
                case 4: return [1, 1, 3, 1];
                case 5: return [1, 1, 2, 1];
                case 6: return [2, 2, 1, 2];
                case 7: return [2, 2, 3, 2];
                case 8: return [3, 3, 1, 3];
                case 9: return [2, 2, 1, 2];
                case 10: return [3, 3, 3, -1];
                case 11: return [2, 2, 2, -1];
                case 12: return [3, 3, 3, -1];
                case 13: return [4, 4, 4, -1];
                case 14: return [1, 2, 3, 1];
                case 15: return [1, 2, 1, -1];
                case 16: case 17: case 18: case 19: return [4, 4, 4, -1];
                case 20: return [3, 3, -1, -1];
                case 21: return [2, 4, 5, -1];
                case 22: return [2, 1, 3, 2];
                case 23: return [2, 3, 2, -1];
                case 24: return [4, 4, 4, -1];
                case 25: case 26: return [1, 2, 3, -1];
                case 27: return [2, 1, 3, -1];
                default: return [3, 3, 3, -1];
            }
        }

        /// <summary>
        /// Gets the attack style types for this weapon.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>An array of attack style type IDs.</returns>
        public static int[] GetAttackStylesTypes(this IItemType itemType)
        {
            int weaponType = itemType.GetWeaponType();
            switch (weaponType)
            {
                case 1: return [1, 2, 3, -1];
                case 2: return [1, 2, 2, 3];
                case 3: return [1, 2, 3, -1];
                case 4: return [1, 2, 2, 3];
                case 5: return [1, 2, 2, 3];
                case 6: return [1, 2, 4, 3];
                case 7: return [1, 2, 2, 3];
                case 8: return [1, 2, 4, 3];
                case 9: return [1, 2, 4, 3];
                case 10: return [1, 2, 3, -1];
                case 11: return [1, 4, 3, -1];
                case 12: return [1, 2, 3, -1];
                case 13: return [5, 6, 7, -1];
                case 14: return [4, 4, 4, 3];
                case 15: return [4, 2, 3, -1];
                case 16: case 17: case 18: case 19: return [5, 6, 7, -1];
                case 20: return [2, 2, -1, -1];
                case 21: return [2, 5, 9, -1];
                case 22: return [1, 2, 2, 3];
                case 23: return [1, 2, 3, -1];
                case 24: return [5, 6, 7, -1];
                case 25: case 26: return [1, 2, 3, -1];
                case 27: return [4, 4, 4, -1];
                default: return [1, 2, 3, -1];
            }
        }

        /// <summary>
        /// Gets the skill level requirements for creating this item.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>A dictionary where the key is the skill ID and the value is the required level, or null if not applicable.</returns>
        public static IReadOnlyDictionary<int, int>? GetCreateItemRequirements(this IItemType itemType)
        {
            if (itemType.ExtraData != null)
            {
                var requirementsBuilder = ImmutableDictionary.CreateBuilder<int, int>();
                var requiredId = -1;
                var requiredCount = -1;

                foreach (var pair in itemType.ExtraData)
                {
                    if (pair.Value is not string)
                    {
                        if (pair.Key >= 538 && pair.Key <= 770)
                        {
                            var value = Convert.ToInt32(pair.Value);
                            if (pair.Key % 2 == 0)
                            {
                                requiredId = value;
                            }
                            else
                            {
                                requiredCount = value;
                            }

                            if (requiredId != -1 && requiredCount != -1)
                            {
                                requirementsBuilder[requiredId == 1 ? requiredCount : requiredId] = requiredId == 1 ? requiredId : requiredCount;
                                requiredId = -1;
                                requiredCount = -1;
                            }
                        }
                    }
                }
                return requirementsBuilder.ToImmutable();
            }
            return null;
        }

        /// <summary>
        /// Gets the skill level requirements for equipping this item.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>A dictionary where the key is the skill ID and the value is the required level.</returns>
        public static IReadOnlyDictionary<int, int> GetEquipmentRequirements(this IItemType itemType)
        {
            var requirements = ImmutableDictionary.CreateBuilder<int, int>();
            if (itemType.ExtraData != null)
            {
                for (var i = 0; i < 10; i++)
                {
                    var skillKey = 749 + (i * 2);
                    var levelKey = 750 + (i * 2);

                    if (itemType.ExtraData.TryGetValue(skillKey, out var skillObj) && itemType.ExtraData.TryGetValue(levelKey, out var levelObj))
                    {
                        if (skillObj is int skill && levelObj is int level && skill < 25 && level <= 120)
                        {
                            requirements.Add(skill, level);
                        }
                    }
                }
                if (itemType.ExtraData.TryGetValue(ItemConstants.MaxedSkillRequirement, out var maxedSkillObj) && maxedSkillObj is int maxedSkill)
                {
                    if (maxedSkill >= 0 && maxedSkill <= 24)
                    {
                        requirements[maxedSkill] = itemType.Id == 19709 ? 120 : 99;
                    }
                }
            }
            return requirements.ToImmutable();
        }

        /// <summary>
        /// Determines whether this item is in its noted form.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns><c>true</c> if the item is noted; otherwise, <c>false</c>.</returns>
        public static bool IsNoted(this IItemType itemType) => itemType.NoteId != -1 && itemType.NoteTemplateId != -1;

        /// <summary>
        /// Determines whether this item can be stacked in a single inventory slot.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns><c>true</c> if the item is stackable; otherwise, <c>false</c>.</returns>
        public static bool IsStackable(this IItemType itemType) => itemType.StackableType == 1 || itemType.IsNoted();

        /// <summary>
        /// Determines whether this item has a model for when it is worn by a character.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns><c>true</c> if the item has a wearable model; otherwise, <c>false</c>.</returns>
        public static bool HasWearModel(this IItemType itemType) => itemType.MaleWornModelId1 >= 0 || itemType.FemaleWornModelId1 >= 0;

        /// <summary>
        /// Gets the degradation behavior of the item when dropped.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The degradation type.</returns>
        public static DegradeType GetDegradeType(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.DegradeType, out var value))
            {
                if (value is int intValue)
                {
                    return (DegradeType)intValue;
                }
            }
            return DegradeType.DropItem;
        }

        /// <summary>
        /// Gets the render animation ID for this item when equipped.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns>The render animation ID, or -1 if not applicable.</returns>
        public static int GetRenderAnimationId(this IItemType itemType)
        {
            if (itemType.ExtraData != null && itemType.ExtraData.TryGetValue(ItemConstants.RenderAnimationId, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }
            }
            return -1;
        }

        /// <summary>
        /// Determines whether the item has a "destroy" option in the inventory.
        /// </summary>
        /// <param name="itemType">The item type to check.</param>
        /// <returns><c>true</c> if the item has a "destroy" option; otherwise, <c>false</c>.</returns>
        public static bool HasDestroyOption(this IItemType itemType)
        {
            foreach (var option in itemType.InventoryOptions)
            {
                if (option != null)
                    if (option.Equals("Destroy"))
                        return true;
            }
            return false;
        }
    }
}