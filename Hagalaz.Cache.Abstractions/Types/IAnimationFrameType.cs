namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>
/// An animation frame (cache index 0: archive = frame set, member = frame; ID = set &lt;&lt; 16 | member). Values are as stored
/// for the slots the frame stores, which can be fewer than its skeleton has; resolving them needs the skeleton.
/// </summary>
public interface IAnimationFrameType : IType
{
    /// <summary>The leading byte, which the client does not use.</summary>
    int Header { get; set; }
    int SkeletonId { get; set; }
    /// <summary>The flag byte of each stored slot: axis bits 1/2/4 and tween flags in bits 3-4.</summary>
    int[] SlotFlags { get; set; }
    /// <summary>The stored value of each axis whose bit is set, per slot.</summary>
    AnimationSlotValues[] SlotValues { get; set; }
}

public readonly record struct AnimationSlotValues(int? X, int? Y, int? Z);
