using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents the implementation of an item type definition.
    /// </summary>
    public class ItemType : IItemType
    {
        /// <inheritdoc />
        public IReadOnlyDictionary<int, object>? ExtraData { get; set; }
        /// <inheritdoc />
        public int Id { get; }
        /// <inheritdoc />
        public int InterfaceModelId { get; set; }
        /// <inheritdoc />
        public string Name { get; set; }
        /// <inheritdoc />
        public int ModelZoom { get; set; }
        /// <inheritdoc />
        public int ModelRotation1 { get; set; }
        /// <inheritdoc />
        public int ModelRotation2 { get; set; }
        /// <inheritdoc />
        public int ModelOffset1 { get; set; }
        /// <inheritdoc />
        public int ModelOffset2 { get; set; }
        /// <inheritdoc />
        public int StackableType { get; set; }
        /// <inheritdoc />
        public int Value { get; set; }
        /// <inheritdoc />
        public bool MembersOnly { get; set; }
        /// <inheritdoc />
        public int MaleWornModelId1 { get; set; }
        /// <inheritdoc />
        public int MaleWornModelId2 { get; set; }
        /// <inheritdoc />
        public int FemaleWornModelId1 { get; set; }
        /// <inheritdoc />
        public int FemaleWornModelId2 { get; set; }
        /// <inheritdoc />
        public string?[] GroundOptions { get; set; }
        /// <inheritdoc />
        public string?[] InventoryOptions { get; set; }
        /// <inheritdoc />
        public int[]? OriginalModelColors { get; set; }
        /// <inheritdoc />
        public int[]? ModifiedModelColors { get; set; }
        /// <inheritdoc />
        public int[]? OriginalTextureColors { get; set; }
        /// <inheritdoc />
        public int[]? ModifiedTextureColors { get; set; }
        /// <inheritdoc />
        public sbyte[]? UnknownArray1 { get; set; }
        /// <summary>
        /// Gets or sets the quest IDs associated with this item.
        /// </summary>
        public int[]? QuestIDs { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this item is unnoted.
        /// </summary>
        public bool Unnoted { get; set; }
        /// <inheritdoc />
        public int MaleWornModelId3 { get; set; }
        /// <inheritdoc />
        public int FemaleWornModelId3 { get; set; }
        /// <summary>
        /// Gets or sets the multi-stack size.
        /// </summary>
        public int MultiStackSize { get; set; }
        /// <inheritdoc />
        public int MaleHeadModel { get; set; }
        /// <inheritdoc />
        public int FemaleHeadModel { get; set; }
        /// <inheritdoc />
        public int MaleHeadModel2 { get; set; }
        /// <inheritdoc />
        public int FemaleHeadModel2 { get; set; }
        /// <inheritdoc />
        public int Zan2D { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt6 { get; set; }
        /// <inheritdoc />
        public int NoteId { get; set; }
        /// <inheritdoc />
        public int NoteTemplateId { get; set; }
        /// <summary>
        /// Gets or sets the stack IDs.
        /// </summary>
        public int[]? StackIds { get; set; }
        /// <summary>
        /// Gets or sets the stack amounts.
        /// </summary>
        public int[]? StackAmounts { get; set; }
        /// <summary>
        /// Gets or sets the X-axis scale.
        /// </summary>
        public int ScaleX { get; set; }
        /// <summary>
        /// Gets or sets the Y-axis scale.
        /// </summary>
        public int ScaleY { get; set; }
        /// <summary>
        /// Gets or sets the Z-axis scale.
        /// </summary>
        public int ScaleZ { get; set; }
        /// <summary>
        /// Gets or sets the ambient lighting value.
        /// </summary>
        public int Ambient { get; set; }
        /// <summary>
        /// Gets or sets the contrast value.
        /// </summary>
        public int Contrast { get; set; }
        /// <inheritdoc />
        public int TeamId { get; set; }
        /// <inheritdoc />
        public int LendId { get; set; }
        /// <inheritdoc />
        public int LendTemplateId { get; set; }
        /// <inheritdoc />
        public int MaleWearXOffset { get; set; }
        /// <inheritdoc />
        public int MaleWearYOffset { get; set; }
        /// <inheritdoc />
        public int MaleWearZOffset { get; set; }
        /// <inheritdoc />
        public int FemaleWearXOffset { get; set; }
        /// <inheritdoc />
        public int FemaleWearYOffset { get; set; }
        /// <inheritdoc />
        public int FemaleWearZOffset { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt18 { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt19 { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt20 { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt21 { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt22 { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt23 { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt24 { get; set; }
        /// <summary>
        /// Gets or sets an unknown integer value.
        /// </summary>
        public int UnknownInt25 { get; set; }
        /// <summary>
        /// Gets or sets the pick size shift.
        /// </summary>
        public int PickSizeShift { get; set; }
        /// <inheritdoc />
        public int BoughtItemId { get; set; }
        /// <inheritdoc />
        public int BoughtTemplateId { get; set; }
        /// <inheritdoc />
        public sbyte EquipSlot { get; set; }
        /// <inheritdoc />
        public sbyte EquipType { get; set; }
        /// <summary>
        /// Gets or sets an unknown equipment-related integer value.
        /// </summary>
        public sbyte SomeEquipInt { get; set; }
        /// <inheritdoc />
        public bool Noted => NoteId != -1 && NoteTemplateId != -1;
        /// <inheritdoc />
        public bool Stackable => StackableType == 1 || Noted;
        /// <summary>
        /// Gets a value indicating whether this item has a wearable model.
        /// </summary>
        public bool HasWearModel => MaleWornModelId1 >= 0 || FemaleWornModelId1 >= 0;
        /// <inheritdoc />
        public DegradeType DegradeType
        {
            get
            {
                if (ExtraData != null)
                {
                    if (!ExtraData.ContainsKey(1397))
                        return DegradeType.DropItem;
                    return (DegradeType)ExtraData[1397];
                }
                return DegradeType.DropItem;
            }
        }
        /// <inheritdoc />
        public int RenderAnimationId
        {
            get
            {
                if (ExtraData != null)
                {
                    if (!ExtraData.ContainsKey(644))
                        return -1;
                    if (ExtraData[644] is int @int)
                        return @int;
                }
                return -1;
            }
        }
        /// <inheritdoc />
        public bool HasDestroyOption
        {
            get
            {
                foreach (var option in InventoryOptions)
                {
                    if (option != null)
                        if (option.Equals("Destroy"))
                            return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemType"/> class.
        /// </summary>
        /// <param name="id">The item ID.</param>
        public ItemType(int id)
        {
            Id = id;
            GroundOptions = new string?[] { null, null, "take", null, null };
            InventoryOptions = new string?[] { null, null, null, null, "drop" };
            Name = "null";
            MaleWornModelId1 = -1;
            FemaleWornModelId1 = -1;
            MaleWornModelId2 = -1;
            FemaleWornModelId2 = -1;
            ModelZoom = 2000;
            LendId = -1;
            LendTemplateId = -1;
            NoteId = -1;
            NoteTemplateId = -1;
            BoughtItemId = -1;
            BoughtTemplateId = -1;
            ScaleZ = 128;
            Value = 1;
            MaleWornModelId3 = -1;
            FemaleWornModelId3 = -1;
            EquipSlot = -1;
            EquipType = -1;
        }

        /// <inheritdoc />
        public void MakeNote(IItemType item, IItemType template)
        {
            MembersOnly = item.MembersOnly;
            InterfaceModelId = template.InterfaceModelId;
            OriginalModelColors = template.OriginalModelColors;
            Name = item.Name;
            ModelOffset2 = template.ModelOffset2;
            OriginalTextureColors = template.OriginalTextureColors;
            Value = item.Value;
            ModelRotation2 = template.ModelRotation2;
            StackableType = 1;
            ModifiedModelColors = template.ModifiedModelColors;
            ModelRotation1 = template.ModelRotation1;
            ModelZoom = template.ModelZoom;
            Zan2D = template.Zan2D;
        }
        /// <inheritdoc />
        public void MakeLend(IItemType item, IItemType template)
        {
            MaleWornModelId2 = item.MaleWornModelId2;
            MaleHeadModel = item.MaleHeadModel;
            FemaleWornModelId1 = item.FemaleWornModelId1;
            MaleWearYOffset = item.MaleWearYOffset;
            MembersOnly = item.MembersOnly;
            InterfaceModelId = template.InterfaceModelId;
            ModifiedTextureColors = item.ModifiedTextureColors;
            FemaleWearXOffset = item.FemaleWearXOffset;
            GroundOptions = item.GroundOptions;
            UnknownArray1 = item.UnknownArray1;
            ModelRotation1 = template.ModelRotation1;
            ModelRotation2 = template.ModelRotation2;
            OriginalModelColors = item.OriginalModelColors;
            Name = item.Name;
            FemaleHeadModel2 = item.FemaleHeadModel2;
            MaleWornModelId1 = item.MaleWornModelId1;
            MaleWearZOffset = item.MaleWearZOffset;
            MaleHeadModel2 = item.MaleHeadModel2;
            MaleWornModelId3 = item.MaleWornModelId3;
            TeamId = item.TeamId;
            ModelOffset2 = template.ModelOffset2;
            ExtraData = item.ExtraData;
            ModifiedModelColors = item.ModifiedModelColors;
            MaleWearXOffset = item.MaleWearXOffset;
            FemaleWornModelId3 = item.FemaleWornModelId3;
            FemaleHeadModel = item.FemaleHeadModel;
            ModelOffset1 = template.ModelOffset1;
            FemaleWearZOffset = item.FemaleWearZOffset;
            OriginalTextureColors = item.OriginalTextureColors;
            Value = 0;
            Zan2D = template.Zan2D;
            ModelZoom = template.ModelZoom;
            FemaleWearYOffset = item.FemaleWearYOffset;
            InventoryOptions = new string[5];
            FemaleWornModelId2 = item.FemaleWornModelId2;
            if (item.InventoryOptions != null)
                for (int i = 0; i < 4; i++)
                    InventoryOptions[i] = item.InventoryOptions[i];
            InventoryOptions[4] = "Discard";
            EquipSlot = item.EquipSlot;
            EquipType = item.EquipType;
        }

        /// <inheritdoc />
        public void MakeBought(IItemType item, IItemType template)
        {
            FemaleWornModelId2 = item.FemaleWornModelId2;
            FemaleWearXOffset = item.FemaleWearXOffset;
            InventoryOptions = new string[5];
            ModelRotation2 = template.ModelRotation2;
            Name = item.Name;
            MaleWornModelId1 = item.MaleWornModelId1;
            ModelOffset2 = template.ModelOffset2;
            MaleWearXOffset = item.MaleWearXOffset;
            MaleWornModelId2 = item.MaleWornModelId2;
            FemaleWornModelId1 = item.FemaleWornModelId1;
            MaleHeadModel = item.MaleHeadModel;
            Zan2D = template.Zan2D;
            ModelOffset1 = template.ModelOffset1;
            UnknownArray1 = item.UnknownArray1;
            StackableType = item.StackableType;
            ModelRotation1 = template.ModelRotation1;
            OriginalTextureColors = item.OriginalTextureColors;
            MaleHeadModel2 = item.MaleHeadModel2;
            FemaleHeadModel2 = item.FemaleHeadModel2;
            MaleWornModelId3 = item.MaleWornModelId3;
            ModifiedTextureColors = item.ModifiedTextureColors;
            FemaleWearZOffset = item.FemaleWearZOffset;
            ModifiedModelColors = item.ModifiedModelColors;
            ModelZoom = template.ModelZoom;
            FemaleWornModelId3 = item.FemaleWornModelId3;
            TeamId = item.TeamId;
            Value = 0;
            GroundOptions = item.GroundOptions;
            OriginalModelColors = item.OriginalModelColors;
            MaleWearYOffset = item.MaleWearYOffset;
            MembersOnly = item.MembersOnly;
            FemaleWearYOffset = item.FemaleWearYOffset;
            ExtraData = item.ExtraData;
            MaleWearZOffset = item.MaleWearZOffset;
            InterfaceModelId = template.InterfaceModelId;
            FemaleHeadModel = item.FemaleHeadModel;
            if (item.InventoryOptions != null)
            {
                for (int i = 0; i < 4; i++)
                    InventoryOptions[i] = item.InventoryOptions[i];
            }
            InventoryOptions[4] = "Discard";
            EquipSlot = item.EquipSlot;
            EquipType = item.EquipType;
        }

        /// <inheritdoc />
        public bool HasSpecialBar()
        {
            if (ExtraData != null)
            {
                if (!ExtraData.ContainsKey(687))
                    return false;
                if (((int)ExtraData[687]) == 1)
                    return true;
            }
            return false;
        }

        /// <inheritdoc />
        public int GetQuestId()
        {
            if (ExtraData != null)
            {
                if (!ExtraData.ContainsKey(861))
                    return -1;
                if (ExtraData[861] is int)
                    return (int)ExtraData[861];
            }
            return -1;
        }

        /// <inheritdoc />
        public int GetAttackSpeed()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(14))
                    return (int)ExtraData[14];
            }
            return 4;
        }

        /// <inheritdoc />
        public int GetStabAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(0))
                    return (int)ExtraData[0];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetSlashAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(1))
                    return (int)ExtraData[1];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetCrushAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(2))
                    return (int)ExtraData[2];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetMagicAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(3))
                    return (int)ExtraData[3];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetRangeAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(4))
                    return (int)ExtraData[4];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetStabDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(5))
                    return (int)ExtraData[5];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetSlashDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(6))
                    return (int)ExtraData[6];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetCrushDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(7))
                    return (int)ExtraData[7];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetMagicDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(8))
                    return (int)ExtraData[8];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetRangeDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(9))
                    return (int)ExtraData[9];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetSummoningDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(417))
                    return (int)ExtraData[417];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetAbsorbMeleeBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(967))
                    return (int)ExtraData[967];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetAbsorbMageBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(969))
                    return (int)ExtraData[969];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetAbsorbRangeBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(968))
                    return (int)ExtraData[968];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetStrengthBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(641))
                    return (int)ExtraData[641] / 10;
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetRangedStrengthBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(643))
                    return (int)ExtraData[643] / 10;
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetMagicDamage()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(685))
                    return (int)ExtraData[685];
            }
            return 0;
        }

        /// <inheritdoc />
        public int GetPrayerBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(11))
                    return (int)ExtraData[11];
            }
            return 0;
        }

        /// <summary>
        /// Gets the weapon type ID.
        /// </summary>
        /// <returns>The weapon type ID.</returns>
        public int GetWeaponType()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(686))
                    return (int)ExtraData[686];
            }
            return 0;
        }

        /// <inheritdoc />
        public int[] GetAttackBonusTypes()
        {
            int weaponType = GetWeaponType();
            switch (weaponType)
            {
                case 1: return new int[] { 3, 3, 3, -1 };
                case 2: return new int[] { 2, 2, 3, 2 };
                case 3: return new int[] { 3, 3, 3, -1 };
                case 4: return new int[] { 1, 1, 3, 1 };
                case 5: return new int[] { 1, 1, 2, 1 };
                case 6: return new int[] { 2, 2, 1, 2 };
                case 7: return new int[] { 2, 2, 3, 2 };
                case 8: return new int[] { 3, 3, 1, 3 };
                case 9: return new int[] { 2, 2, 1, 2 };
                case 10: return new int[] { 3, 3, 3, -1 };
                case 11: return new int[] { 2, 2, 2, -1 };
                case 12: return new int[] { 3, 3, 3, -1 };
                case 13: return new int[] { 4, 4, 4, -1 };
                case 14: return new int[] { 1, 2, 3, 1 };
                case 15: return new int[] { 1, 2, 1, -1 };
                case 16: case 17: case 18: case 19: return new int[] { 4, 4, 4, -1 };
                case 20: return new int[] { 3, 3, -1, -1 };
                case 21: return new int[] { 2, 4, 5, -1 };
                case 22: return new int[] { 2, 1, 3, 2 };
                case 23: return new int[] { 2, 3, 2, -1 };
                case 24: return new int[] { 4, 4, 4, -1 };
                case 25: case 26: return new int[] { 1, 2, 3, -1 };
                case 27: return new int[] { 2, 1, 3, -1 };
                default: return new int[] { 3, 3, 3, -1 };
            }
        }

        /// <inheritdoc />
        public int[] GetAttackStylesTypes()
        {
            int weaponType = GetWeaponType();
            switch (weaponType)
            {
                case 1: return new int[] { 1, 2, 3, -1 };
                case 2: return new int[] { 1, 2, 2, 3 };
                case 3: return new int[] { 1, 2, 3, -1 };
                case 4: return new int[] { 1, 2, 2, 3 };
                case 5: return new int[] { 1, 2, 2, 3 };
                case 6: return new int[] { 1, 2, 4, 3 };
                case 7: return new int[] { 1, 2, 2, 3 };
                case 8: return new int[] { 1, 2, 4, 3 };
                case 9: return new int[] { 1, 2, 4, 3 };
                case 10: return new int[] { 1, 2, 3, -1 };
                case 11: return new int[] { 1, 4, 3, -1 };
                case 12: return new int[] { 1, 2, 3, -1 };
                case 13: return new int[] { 5, 6, 7, -1 };
                case 14: return new int[] { 4, 4, 4, 3 };
                case 15: return new int[] { 4, 2, 3, -1 };
                case 16: case 17: case 18: case 19: return new int[] { 5, 6, 7, -1 };
                case 20: return new int[] { 2, 2, -1, -1 };
                case 21: return new int[] { 2, 5, 9, -1 };
                case 22: return new int[] { 1, 2, 2, 3 };
                case 23: return new int[] { 1, 2, 3, -1 };
                case 24: return new int[] { 5, 6, 7, -1 };
                case 25: case 26: return new int[] { 1, 2, 3, -1 };
                case 27: return new int[] { 4, 4, 4, -1 };
                default: return new int[] { 1, 2, 3, -1 };
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<int, int>? GetCreateItemRequirements()
        {
            if (ExtraData != null)
            {
                var requirementsBuilder = ImmutableDictionary.CreateBuilder<int, int>();
                var requiredId = -1;
                var requiredCount = -1;

                foreach (var pair in ExtraData)
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

        /// <inheritdoc />
        public IReadOnlyDictionary<int, int> GetEquipmentRequirements()
        {
            var requirements = ImmutableDictionary.CreateBuilder<int, int>();
            if (ExtraData != null)
            {
                for (var i = 0; i < 10; i++)
                {
                    var skillKey = 749 + (i * 2);
                    var levelKey = 750 + (i * 2);

                    if (ExtraData.TryGetValue(skillKey, out var skillObj) && ExtraData.TryGetValue(levelKey, out var levelObj))
                    {
                        if (skillObj is int skill && levelObj is int level && skill < 25 && level <= 120)
                        {
                            requirements.Add(skill, level);
                        }
                    }
                }
                if (ExtraData.TryGetValue(277, out var maxedSkillObj) && maxedSkillObj is int maxedSkill)
                {
                    if (maxedSkill >= 0 && maxedSkill <= 24)
                    {
                        requirements[maxedSkill] = Id == 19709 ? 120 : 99;
                    }
                }
            }
            return requirements.ToImmutable();
        }
    }
}