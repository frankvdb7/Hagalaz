using System.Collections.Generic;
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
            GroundOptions = [null, null, "take", null, null];
            InventoryOptions = [null, null, null, null, "drop"];
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
            ScaleX = 128;
            ScaleY = 128;
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
            // Set properties from the original item
            MembersOnly = item.MembersOnly;
            Value = item.Value;
            Name = item.Name;
            StackableType = 1;

            // Set properties from the template
            InterfaceModelId = template.InterfaceModelId;
            ModelZoom = template.ModelZoom;
            ModelRotation1 = template.ModelRotation1;
            ModelRotation2 = template.ModelRotation2;
            ModelOffset1 = template.ModelOffset1;
            ModelOffset2 = template.ModelOffset2;
            Zan2D = template.Zan2D;

            OriginalModelColors = template.OriginalModelColors;
            ModifiedModelColors = template.ModifiedModelColors;
            OriginalTextureColors = template.OriginalTextureColors;
            ModifiedTextureColors = template.ModifiedTextureColors;
        }
        /// <inheritdoc />
        public void MakeLend(IItemType item, IItemType template)
        {
            // Set properties from the original item
            MaleWornModelId1 = item.MaleWornModelId1;
            MaleWornModelId2 = item.MaleWornModelId2;
            MaleWornModelId3 = item.MaleWornModelId3;
            FemaleWornModelId1 = item.FemaleWornModelId1;
            FemaleWornModelId2 = item.FemaleWornModelId2;
            FemaleWornModelId3 = item.FemaleWornModelId3;
            MaleHeadModel = item.MaleHeadModel;
            MaleHeadModel2 = item.MaleHeadModel2;
            FemaleHeadModel = item.FemaleHeadModel;
            FemaleHeadModel2 = item.FemaleHeadModel2;
            MaleWearXOffset = item.MaleWearXOffset;
            MaleWearYOffset = item.MaleWearYOffset;
            MaleWearZOffset = item.MaleWearZOffset;
            FemaleWearXOffset = item.FemaleWearXOffset;
            FemaleWearYOffset = item.FemaleWearYOffset;
            FemaleWearZOffset = item.FemaleWearZOffset;
            EquipSlot = item.EquipSlot;
            EquipType = item.EquipType;
            GroundOptions = item.GroundOptions;
            UnknownArray1 = item.UnknownArray1;
            TeamId = item.TeamId;
            ExtraData = item.ExtraData;
            MembersOnly = item.MembersOnly;
            Name = item.Name;
            Value = 0;

            // Set properties from the template
            InterfaceModelId = template.InterfaceModelId;
            ModelZoom = template.ModelZoom;
            ModelRotation1 = template.ModelRotation1;
            ModelRotation2 = template.ModelRotation2;
            ModelOffset1 = template.ModelOffset1;
            ModelOffset2 = template.ModelOffset2;
            Zan2D = template.Zan2D;

            // Set colors and textures from the item
            OriginalModelColors = item.OriginalModelColors;
            ModifiedModelColors = item.ModifiedModelColors;
            OriginalTextureColors = item.OriginalTextureColors;
            ModifiedTextureColors = item.ModifiedTextureColors;

            // Set inventory options
            InventoryOptions = new string[5];
            if (item.InventoryOptions != null)
            {
                for (int i = 0; i < 4; i++)
                    InventoryOptions[i] = item.InventoryOptions[i];
            }
            InventoryOptions[4] = "Discard";
        }

        /// <inheritdoc />
        public void MakeBought(IItemType item, IItemType template)
        {
            // Set properties from the original item
            MaleWornModelId1 = item.MaleWornModelId1;
            MaleWornModelId2 = item.MaleWornModelId2;
            MaleWornModelId3 = item.MaleWornModelId3;
            FemaleWornModelId1 = item.FemaleWornModelId1;
            FemaleWornModelId2 = item.FemaleWornModelId2;
            FemaleWornModelId3 = item.FemaleWornModelId3;
            MaleHeadModel = item.MaleHeadModel;
            MaleHeadModel2 = item.MaleHeadModel2;
            FemaleHeadModel = item.FemaleHeadModel;
            FemaleHeadModel2 = item.FemaleHeadModel2;
            MaleWearXOffset = item.MaleWearXOffset;
            MaleWearYOffset = item.MaleWearYOffset;
            MaleWearZOffset = item.MaleWearZOffset;
            FemaleWearXOffset = item.FemaleWearXOffset;
            FemaleWearYOffset = item.FemaleWearYOffset;
            FemaleWearZOffset = item.FemaleWearZOffset;
            EquipSlot = item.EquipSlot;
            EquipType = item.EquipType;
            GroundOptions = item.GroundOptions;
            UnknownArray1 = item.UnknownArray1;
            TeamId = item.TeamId;
            ExtraData = item.ExtraData;
            MembersOnly = item.MembersOnly;
            Name = item.Name;
            StackableType = item.StackableType;
            Value = 0;

            // Set properties from the template
            InterfaceModelId = template.InterfaceModelId;
            ModelZoom = template.ModelZoom;
            ModelRotation1 = template.ModelRotation1;
            ModelRotation2 = template.ModelRotation2;
            ModelOffset1 = template.ModelOffset1;
            ModelOffset2 = template.ModelOffset2;
            Zan2D = template.Zan2D;

            // Set colors and textures from the item
            OriginalModelColors = item.OriginalModelColors;
            ModifiedModelColors = item.ModifiedModelColors;
            OriginalTextureColors = item.OriginalTextureColors;
            ModifiedTextureColors = item.ModifiedTextureColors;

            // Set inventory options
            InventoryOptions = new string[5];
            if (item.InventoryOptions != null)
            {
                for (int i = 0; i < 4; i++)
                    InventoryOptions[i] = item.InventoryOptions[i];
            }
            InventoryOptions[4] = "Discard";
        }

    }
}