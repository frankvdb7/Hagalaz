using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class AnimationSkeletonType(int id) : IAnimationSkeletonType
{
    public int Id { get; } = id;
    public int[] Types { get; set; } = [];
    public int[] Enabled { get; set; } = [];
    public int[] Masks { get; set; } = [];
    public int[][] Groups { get; set; } = [];
}
