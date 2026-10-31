namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetItemTypeDto(
        int Id,
        string Name,
        int Value,
        bool Stackable,
        bool Noted,
        int NoteId,
        bool MembersOnly,
        sbyte EquipSlot,
        sbyte EquipType,
        int RenderAnimationId,
        string?[] InventoryOptions,
        string?[] GroundOptions,
        int NoteTemplateId,
        int LendId,
        int LendTemplateId,
        int BoughtItemId,
        int BoughtTemplateId,
        int InterfaceModelId);
