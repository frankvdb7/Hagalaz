using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Hagalaz.Cache.Types;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record UpdateItemDefinitionDto
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

    /// <summary>
    /// Returns a validation error, or null when <see cref="ApplyTo"/> can encode this definition without truncating counts,
    /// colliding opcodes or dropping half of a paired list.
    /// </summary>
    public string? Validate(int id)
    {
        if (id < 0 || Id != id) return "Provide matching nonnegative IDs.";
        if (Value < 0) return "Base value must be nonnegative.";
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 50) return "Display name must be 1-50 characters.";
        if (!IsPrintableLatin1(Name)) return "Display name must contain printable Latin-1 characters.";
        // Ground options use opcodes 30-34 and inventory options 35-39, so a sixth entry would overwrite the next list.
        if (GroundOptions is not { Length: <= 5 } || InventoryOptions is not { Length: <= 5 })
            return "Ground and inventory options must each be a list of at most 5 entries.";
        if (GroundOptions.Concat(InventoryOptions).Any(option => option is not null && !IsPrintableLatin1(option)))
            return "Options must contain printable Latin-1 characters.";
        if (PairError(OriginalModelColors, ModifiedModelColors, byte.MaxValue, "Model recolors") is { } modelColors) return modelColors;
        if (PairError(OriginalTextureColors, ModifiedTextureColors, byte.MaxValue, "Texture recolors") is { } textureColors) return textureColors;
        // Stack variants use opcodes 100-109.
        if (PairError(StackIds, StackAmounts, 10, "Stack variants") is { } stacks) return stacks;
        if (UnknownArray1 is { Length: > byte.MaxValue } || QuestIDs is { Length: > byte.MaxValue })
            return "Palette recolors and quest IDs must each have at most 255 entries.";
        if (ExtraData is { Count: > byte.MaxValue }) return "Extra data must have at most 255 entries.";
        if (ExtraData?.Values.Any(value => !IsExtraDataValue(value)) == true)
            return "Extra data values must be 32-bit integers or printable Latin-1 strings.";
        return null;

        static string? PairError<T1, T2>(T1[]? first, T2[]? second, int limit, string name) =>
            (first is null) != (second is null) ? $"{name} must provide both lists or neither."
            : first is not null && first.Length != second!.Length ? $"{name} lists must have the same length."
            : first is { Length: var length } && length > limit ? $"{name} must have at most {limit} entries."
            : null;

        static bool IsExtraDataValue(object? value) => value is JsonElement element && element.ValueKind switch
        {
            JsonValueKind.String => IsPrintableLatin1(element.GetString()!),
            JsonValueKind.Number => element.TryGetInt32(out _),
            _ => false,
        };
    }

    private static bool IsPrintableLatin1(string value) => value.All(character => character is >= ' ' and <= (char)byte.MaxValue);

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
