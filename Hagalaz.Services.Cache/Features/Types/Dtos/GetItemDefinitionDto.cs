using System.Collections.Generic;
using Hagalaz.Cache.Types;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetItemDefinitionDto
{
    public int Id { get; init; }
    public IReadOnlyDictionary<int, object>? ExtraData { get; init; }
    public int InterfaceModelId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int ModelZoom { get; init; }
    public int ModelRotation1 { get; init; }
    public int ModelRotation2 { get; init; }
    public int ModelOffset1 { get; init; }
    public int ModelOffset2 { get; init; }
    public int StackableType { get; init; }
    public int Value { get; init; }
    public bool MembersOnly { get; init; }
    public int MaleWornModelId1 { get; init; }
    public int MaleWornModelId2 { get; init; }
    public int FemaleWornModelId1 { get; init; }
    public int FemaleWornModelId2 { get; init; }
    public string?[] GroundOptions { get; init; } = [];
    public string?[] InventoryOptions { get; init; } = [];
    public int[]? OriginalModelColors { get; init; }
    public int[]? ModifiedModelColors { get; init; }
    public int[]? OriginalTextureColors { get; init; }
    public int[]? ModifiedTextureColors { get; init; }
    public sbyte[]? UnknownArray1 { get; init; }
    public int[]? QuestIDs { get; init; }
    public bool Unnoted { get; init; }
    public int MaleWornModelId3 { get; init; }
    public int FemaleWornModelId3 { get; init; }
    public int MultiStackSize { get; init; }
    public int MaleHeadModel { get; init; }
    public int FemaleHeadModel { get; init; }
    public int MaleHeadModel2 { get; init; }
    public int FemaleHeadModel2 { get; init; }
    public int Zan2D { get; init; }
    public int UnknownInt6 { get; init; }
    public int NoteId { get; init; }
    public int NoteTemplateId { get; init; }
    public int[]? StackIds { get; init; }
    public int[]? StackAmounts { get; init; }
    public int ScaleX { get; init; }
    public int ScaleY { get; init; }
    public int ScaleZ { get; init; }
    public int Ambient { get; init; }
    public int Contrast { get; init; }
    public int TeamId { get; init; }
    public int LendId { get; init; }
    public int LendTemplateId { get; init; }
    public int MaleWearXOffset { get; init; }
    public int MaleWearYOffset { get; init; }
    public int MaleWearZOffset { get; init; }
    public int FemaleWearXOffset { get; init; }
    public int FemaleWearYOffset { get; init; }
    public int FemaleWearZOffset { get; init; }
    public int UnknownInt18 { get; init; }
    public int UnknownInt19 { get; init; }
    public int UnknownInt20 { get; init; }
    public int UnknownInt21 { get; init; }
    public int UnknownInt22 { get; init; }
    public int UnknownInt23 { get; init; }
    public int UnknownInt24 { get; init; }
    public int UnknownInt25 { get; init; }
    public int PickSizeShift { get; init; }
    public int BoughtItemId { get; init; }
    public int BoughtTemplateId { get; init; }
    public sbyte EquipSlot { get; init; }
    public sbyte EquipType { get; init; }
    public sbyte SomeEquipInt { get; init; }
    public bool Noted { get; init; }
    public bool Stackable { get; init; }
    public bool HasWearModel { get; init; }
    public int DegradeType { get; init; }
    public int RenderAnimationId { get; init; }
    public bool HasDestroyOption { get; init; }

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
