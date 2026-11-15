using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

/// <summary>The stored, editable fields of an item definition, shared by the read and the full-replacement contracts.</summary>
public abstract record ItemDefinitionFieldsDto
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
}
