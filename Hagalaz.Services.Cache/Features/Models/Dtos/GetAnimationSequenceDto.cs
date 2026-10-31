namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record GetAnimationSequenceDto(int Id, int[] FrameIds, int[] FrameDurations, int Loop, bool IsTweened);
