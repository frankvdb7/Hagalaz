using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Exposes a container's composed storage to shared infrastructure.</summary>
public interface IItemContainerStorageProvider
{
    ItemContainerStorage Storage { get; }

    /// <summary>Publishes a committed container mutation through its owning domain.</summary>
    void PublishChanges(HashSet<int>? changedSlots);
}
