using System.Collections.Generic;
using Hagalaz.Cache.Types;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed class GetItemDefinitionDto
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
    public bool Noted { get; set; }
    public bool Stackable { get; set; }
    public bool HasWearModel { get; set; }
    public int DegradeType { get; set; }
    public int RenderAnimationId { get; set; }
    public bool HasDestroyOption { get; set; }

    public static GetItemDefinitionDto FromItem(ItemType item) => new()
    {
        Id = item.Id,
        ExtraData = item.ExtraData,
        InterfaceModelId = item.InterfaceModelId,
        Name = item.Name,
        ModelZoom = item.ModelZoom,
        ModelRotation1 = item.ModelRotation1,
        ModelRotation2 = item.ModelRotation2,
        ModelOffset1 = item.ModelOffset1,
        ModelOffset2 = item.ModelOffset2,
        StackableType = item.StackableType,
        Value = item.Value,
        MembersOnly = item.MembersOnly,
        MaleWornModelId1 = item.MaleWornModelId1,
        MaleWornModelId2 = item.MaleWornModelId2,
        FemaleWornModelId1 = item.FemaleWornModelId1,
        FemaleWornModelId2 = item.FemaleWornModelId2,
        GroundOptions = item.GroundOptions,
        InventoryOptions = item.InventoryOptions,
        OriginalModelColors = item.OriginalModelColors,
        ModifiedModelColors = item.ModifiedModelColors,
        OriginalTextureColors = item.OriginalTextureColors,
        ModifiedTextureColors = item.ModifiedTextureColors,
        UnknownArray1 = item.UnknownArray1,
        QuestIDs = item.QuestIDs,
        Unnoted = item.Unnoted,
        MaleWornModelId3 = item.MaleWornModelId3,
        FemaleWornModelId3 = item.FemaleWornModelId3,
        MultiStackSize = item.MultiStackSize,
        MaleHeadModel = item.MaleHeadModel,
        FemaleHeadModel = item.FemaleHeadModel,
        MaleHeadModel2 = item.MaleHeadModel2,
        FemaleHeadModel2 = item.FemaleHeadModel2,
        Zan2D = item.Zan2D,
        UnknownInt6 = item.UnknownInt6,
        NoteId = item.NoteId,
        NoteTemplateId = item.NoteTemplateId,
        StackIds = item.StackIds,
        StackAmounts = item.StackAmounts,
        ScaleX = item.ScaleX,
        ScaleY = item.ScaleY,
        ScaleZ = item.ScaleZ,
        Ambient = item.Ambient,
        Contrast = item.Contrast,
        TeamId = item.TeamId,
        LendId = item.LendId,
        LendTemplateId = item.LendTemplateId,
        MaleWearXOffset = item.MaleWearXOffset,
        MaleWearYOffset = item.MaleWearYOffset,
        MaleWearZOffset = item.MaleWearZOffset,
        FemaleWearXOffset = item.FemaleWearXOffset,
        FemaleWearYOffset = item.FemaleWearYOffset,
        FemaleWearZOffset = item.FemaleWearZOffset,
        UnknownInt18 = item.UnknownInt18,
        UnknownInt19 = item.UnknownInt19,
        UnknownInt20 = item.UnknownInt20,
        UnknownInt21 = item.UnknownInt21,
        UnknownInt22 = item.UnknownInt22,
        UnknownInt23 = item.UnknownInt23,
        UnknownInt24 = item.UnknownInt24,
        UnknownInt25 = item.UnknownInt25,
        PickSizeShift = item.PickSizeShift,
        BoughtItemId = item.BoughtItemId,
        BoughtTemplateId = item.BoughtTemplateId,
        EquipSlot = item.EquipSlot,
        EquipType = item.EquipType,
        SomeEquipInt = item.SomeEquipInt,
        Noted = item.Noted,
        Stackable = item.Stackable,
        HasWearModel = item.HasWearModel,
        DegradeType = (int)item.DegradeType,
        RenderAnimationId = item.RenderAnimationId,
        HasDestroyOption = item.HasDestroyOption,
    };
}
