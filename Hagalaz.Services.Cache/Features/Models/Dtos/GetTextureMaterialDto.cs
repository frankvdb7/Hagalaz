namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record GetTextureMaterialDto(int AlphaMode, int ColorBlend, int Brightness, int EffectId, int EffectParameter,
    int ScrollU, int ScrollV, bool RepeatU, bool RepeatV, bool HasHdr, int EffectParameter2 = 0);
