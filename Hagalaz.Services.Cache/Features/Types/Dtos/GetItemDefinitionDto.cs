using Hagalaz.Cache.Types;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

/// <summary>An item definition as stored, plus the read-only values derived from it.</summary>
public sealed record GetItemDefinitionDto : ItemDefinitionFieldsDto
{
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
