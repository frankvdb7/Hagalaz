namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record GetItemModelAppearanceDto(string Key, string Label, int[] ModelIds, int OffsetX = 0, int OffsetY = 0, int OffsetZ = 0)
{
    public bool Available => ModelIds.Length > 0;
}
