using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Provides storage access and committed-change publication to cross-container coordination.
/// </summary>
/// <remarks>This is an infrastructure contract, not the gameplay-facing item-container API.</remarks>
public interface IItemContainerStorageOwner
{
    ItemContainerStorage Storage { get; }

    /// <summary>Publishes a committed cross-container mutation through its owning domain.</summary>
    void PublishChanges(HashSet<int>? changedSlots);
}
