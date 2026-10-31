using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Hagalaz.Cache.Types;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed class UpdateItemDefinitionDto
{
    public int Id { get; set; }
    public IReadOnlyDictionary<int, object>? ExtraData { get; set; }
    public int InterfaceModelId { get; set; }
    public string Name { get; set; } = string.Empty;
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
    public string?[] GroundOptions { get; set; } = [];
    public string?[] InventoryOptions { get; set; } = [];
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

    public void ApplyTo(ItemType item)
    {
        item.ExtraData = ExtraData?.ToDictionary(pair => pair.Key, pair => pair.Value is JsonElement value
            ? value.ValueKind == JsonValueKind.String ? (object)value.GetString()! : value.GetInt32()
            : pair.Value);
        item.InterfaceModelId = InterfaceModelId;
        item.Name = Name;
        item.ModelZoom = ModelZoom;
        item.ModelRotation1 = ModelRotation1;
        item.ModelRotation2 = ModelRotation2;
        item.ModelOffset1 = ModelOffset1;
        item.ModelOffset2 = ModelOffset2;
        item.StackableType = StackableType;
        item.Value = Value;
        item.MembersOnly = MembersOnly;
        item.MaleWornModelId1 = MaleWornModelId1;
        item.MaleWornModelId2 = MaleWornModelId2;
        item.FemaleWornModelId1 = FemaleWornModelId1;
        item.FemaleWornModelId2 = FemaleWornModelId2;
        item.GroundOptions = GroundOptions;
        item.InventoryOptions = InventoryOptions;
        item.OriginalModelColors = OriginalModelColors;
        item.ModifiedModelColors = ModifiedModelColors;
        item.OriginalTextureColors = OriginalTextureColors;
        item.ModifiedTextureColors = ModifiedTextureColors;
        item.UnknownArray1 = UnknownArray1;
        item.QuestIDs = QuestIDs;
        item.Unnoted = Unnoted;
        item.MaleWornModelId3 = MaleWornModelId3;
        item.FemaleWornModelId3 = FemaleWornModelId3;
        item.MultiStackSize = MultiStackSize;
        item.MaleHeadModel = MaleHeadModel;
        item.FemaleHeadModel = FemaleHeadModel;
        item.MaleHeadModel2 = MaleHeadModel2;
        item.FemaleHeadModel2 = FemaleHeadModel2;
        item.Zan2D = Zan2D;
        item.UnknownInt6 = UnknownInt6;
        item.NoteId = NoteId;
        item.NoteTemplateId = NoteTemplateId;
        item.StackIds = StackIds;
        item.StackAmounts = StackAmounts;
        item.ScaleX = ScaleX;
        item.ScaleY = ScaleY;
        item.ScaleZ = ScaleZ;
        item.Ambient = Ambient;
        item.Contrast = Contrast;
        item.TeamId = TeamId;
        item.LendId = LendId;
        item.LendTemplateId = LendTemplateId;
        item.MaleWearXOffset = MaleWearXOffset;
        item.MaleWearYOffset = MaleWearYOffset;
        item.MaleWearZOffset = MaleWearZOffset;
        item.FemaleWearXOffset = FemaleWearXOffset;
        item.FemaleWearYOffset = FemaleWearYOffset;
        item.FemaleWearZOffset = FemaleWearZOffset;
        item.UnknownInt18 = UnknownInt18;
        item.UnknownInt19 = UnknownInt19;
        item.UnknownInt20 = UnknownInt20;
        item.UnknownInt21 = UnknownInt21;
        item.UnknownInt22 = UnknownInt22;
        item.UnknownInt23 = UnknownInt23;
        item.UnknownInt24 = UnknownInt24;
        item.UnknownInt25 = UnknownInt25;
        item.PickSizeShift = PickSizeShift;
        item.BoughtItemId = BoughtItemId;
        item.BoughtTemplateId = BoughtTemplateId;
        item.EquipSlot = EquipSlot;
        item.EquipType = EquipType;
        item.SomeEquipInt = SomeEquipInt;
    }
}
