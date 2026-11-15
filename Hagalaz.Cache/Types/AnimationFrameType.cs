using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class AnimationFrameType(int id) : IAnimationFrameType
{
    public int Id { get; } = id;
    public int Header { get; set; } = 1;
    public int SkeletonId { get; set; }
    public int[] SlotFlags { get; set; } = [];
    public AnimationSlotValues[] SlotValues { get; set; } = [];
}
