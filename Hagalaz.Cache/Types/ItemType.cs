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
        public IReadOnlyDictionary<int, object>? ExtraData { get; set; }
        internal byte[][]? PreservedOpcodes { get; set; }
        public int Id { get; }
        public int InterfaceModelId { get; set; }
        public string Name { get; set; }
        public int ModelZoom { get; set; }
        public int ModelRotation1 { get; set; }
        public int ModelRotation2 { get; set; }
        public int ModelOffset1 { get; set; }
        public int ModelOffset2 { get; set; }
        public int StackableType { get; set; }
        public int Value { get; set; }
        public bool MembersOnly { get; set; }
        public int MaleWornModelId1 { get; set; }
        public int MaleWornModelId2 { get; set; }
        public int FemaleWornModelId1 { get; set; }
        public int FemaleWornModelId2 { get; set; }
        public string?[] GroundOptions { get; set; }
        public string?[] InventoryOptions { get; set; }
        public int[]? OriginalModelColors { get; set; }
        public int[]? ModifiedModelColors { get; set; }
        public int[]? OriginalTextureColors { get; set; }
        public int[]? ModifiedTextureColors { get; set; }
        public sbyte[]? UnknownArray1 { get; set; }
        public int[]? QuestIDs { get; set; }
        public bool Unnoted { get; set; }
        public int MaleWornModelId3 { get; set; }
        public int FemaleWornModelId3 { get; set; }
        public int MultiStackSize { get; set; }
        public int MaleHeadModel { get; set; }
        public int FemaleHeadModel { get; set; }
        public int MaleHeadModel2 { get; set; }
        public int FemaleHeadModel2 { get; set; }
        public int Zan2D { get; set; }
        public int UnknownInt6 { get; set; }
        public int NoteId { get; set; }
        public int NoteTemplateId { get; set; }
        public int[]? StackIds { get; set; }
        public int[]? StackAmounts { get; set; }
        public int ScaleX { get; set; }
        public int ScaleY { get; set; }
        public int ScaleZ { get; set; }
        public int Ambient { get; set; }
        public int Contrast { get; set; }
        public int TeamId { get; set; }
        public int LendId { get; set; }
        public int LendTemplateId { get; set; }
        public int MaleWearXOffset { get; set; }
        public int MaleWearYOffset { get; set; }
        public int MaleWearZOffset { get; set; }
        public int FemaleWearXOffset { get; set; }
        public int FemaleWearYOffset { get; set; }
        public int FemaleWearZOffset { get; set; }
        public int UnknownInt18 { get; set; }
        public int UnknownInt19 { get; set; }
        public int UnknownInt20 { get; set; }
        public int UnknownInt21 { get; set; }
        public int UnknownInt22 { get; set; }
        public int UnknownInt23 { get; set; }
        public int UnknownInt24 { get; set; }
        public int UnknownInt25 { get; set; }
        public int PickSizeShift { get; set; }
        public int BoughtItemId { get; set; }
        public int BoughtTemplateId { get; set; }
        public sbyte EquipSlot { get; set; }
        public sbyte EquipType { get; set; }
        public sbyte SomeEquipInt { get; set; }
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