namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetEquipmentDefaultsDto(int Id, byte[] BodySlotData, int MainHandSlot, int[] WeaponData, int[] ShieldData, int OffHandSlot);
