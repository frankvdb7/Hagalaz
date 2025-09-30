using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents the implementation of an item type definition.
    /// </summary>
    public class ItemType : IItemType
    {
        public IReadOnlyDictionary<int, object>? ExtraData { get; internal set; }
        public int Id { get; }
        public int InterfaceModelId { get; internal set; }
        public string Name { get; internal set; }
        public int ModelZoom { get; internal set; }
        public int ModelRotation1 { get; internal set; }
        public int ModelRotation2 { get; internal set; }
        public int ModelOffset1 { get; internal set; }
        public int ModelOffset2 { get; internal set; }
        public int StackableType { get; internal set; }
        public int Value { get; internal set; }
        public bool MembersOnly { get; internal set; }
        public int MaleWornModelId1 { get; internal set; }
        public int MaleWornModelId2 { get; internal set; }
        public int FemaleWornModelId1 { get; internal set; }
        public int FemaleWornModelId2 { get; internal set; }
        public string?[] GroundOptions { get; internal set; }
        public string?[] InventoryOptions { get; internal set; }
        public int[]? OriginalModelColors { get; internal set; }
        public int[]? ModifiedModelColors { get; internal set; }
        public int[]? OriginalTextureColors { get; internal set; }
        public int[]? ModifiedTextureColors { get; internal set; }
        public sbyte[]? UnknownArray1 { get; internal set; }
        public int[]? QuestIDs { get; internal set; }
        public bool Unnoted { get; internal set; }
        public int MaleWornModelId3 { get; internal set; }
        public int FemaleWornModelId3 { get; internal set; }
        public int MultiStackSize { get; internal set; }
        public int MaleHeadModel { get; internal set; }
        public int FemaleHeadModel { get; internal set; }
        public int MaleHeadModel2 { get; internal set; }
        public int FemaleHeadModel2 { get; internal set; }
        public int Zan2D { get; internal set; }
        public int UnknownInt6 { get; internal set; }
        public int NoteId { get; internal set; }
        public int NoteTemplateId { get; internal set; }
        public int[]? StackIds { get; internal set; }
        public int[]? StackAmounts { get; internal set; }
        public int ScaleX { get; internal set; }
        public int ScaleY { get; internal set; }
        public int ScaleZ { get; internal set; }
        public int Ambient { get; internal set; }
        public int Contrast { get; internal set; }
        public int TeamId { get; internal set; }
        public int LendId { get; internal set; }
        public int LendTemplateId { get; internal set; }
        public int MaleWearXOffset { get; internal set; }
        public int MaleWearYOffset { get; internal set; }
        public int MaleWearZOffset { get; internal set; }
        public int FemaleWearXOffset { get; internal set; }
        public int FemaleWearYOffset { get; internal set; }
        public int FemaleWearZOffset { get; internal set; }
        public int UnknownInt18 { get; internal set; }
        public int UnknownInt19 { get; internal set; }
        public int UnknownInt20 { get; internal set; }
        public int UnknownInt21 { get; internal set; }
        public int UnknownInt22 { get; internal set; }
        public int UnknownInt23 { get; internal set; }
        public int UnknownInt24 { get; internal set; }
        public int UnknownInt25 { get; internal set; }
        public int PickSizeShift { get; internal set; }
        public int BoughtItemId { get; internal set; }
        public int BoughtTemplateId { get; internal set; }
        public sbyte EquipSlot { get; internal set; }
        public sbyte EquipType { get; internal set; }
        public sbyte SomeEquipInt { get; internal set; }
        public bool Noted => ItemTypeLogic.IsNoted(this);
        public bool Stackable => ItemTypeLogic.IsStackable(this);
        public bool HasWearModel => ItemTypeLogic.HasWearModel(this);
        public DegradeType DegradeType => ItemTypeLogic.GetDegradeType(this);
        public int RenderAnimationId => ItemTypeLogic.GetRenderAnimationId(this);
        public bool HasDestroyOption => ItemTypeLogic.HasDestroyOption(this);

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

    }
}