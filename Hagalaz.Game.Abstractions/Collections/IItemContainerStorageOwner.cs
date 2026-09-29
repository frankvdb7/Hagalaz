using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Exposes a container's composed storage to shared infrastructure.</summary>
public interface IItemContainerStorageOwner : IContainer<IItem?>
{
    ItemContainerStorage Storage { get; }

    IItem? IContainer<IItem?>.this[int index] => Storage[index];

    int IContainer<IItem?>.Capacity => Storage.Capacity;

    IEnumerator<IItem?> IEnumerable<IItem?>.GetEnumerator() => Storage.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Storage.GetEnumerator();

    /// <summary>Publishes a committed container mutation through its owning domain.</summary>
    void PublishChanges(HashSet<int>? changedSlots);
}
