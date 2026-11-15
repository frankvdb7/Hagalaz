namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record AnimationSkeletonDto(int[] Types, bool[] Enabled, int[] Masks, int[][] Groups);
